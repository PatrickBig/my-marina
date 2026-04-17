using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using MyMarina.Application.Invoices;
using MyMarina.Application.Tenants;
using MyMarina.Domain.Enums;

namespace MyMarina.IntegrationTests;

/// <summary>
/// Tests for invoice CRUD, line item management, status transitions, and payment recording.
/// </summary>
public class InvoiceTests(ApiWebApplicationFactory factory) : IClassFixture<ApiWebApplicationFactory>
{
    private readonly HttpClient _platformClient = factory.CreatePlatformOperatorClient();

    /// <summary>
    /// Creates tenant + customer account. Returns an owner HttpClient and the customer account ID.
    /// </summary>
    private async Task<(HttpClient Owner, Guid CustomerAccountId)> SetupAsync()
    {
        var slug = $"inv-{Guid.NewGuid():N}"[..28];
        var tenantResp = await _platformClient.PostAsJsonAsync("/tenants", new
        {
            Name = "Invoice Co", Slug = slug,
            OwnerEmail = $"{Guid.NewGuid():N}@inv.io", OwnerFirstName = "I", OwnerLastName = "O",
            OwnerPassword = "OwnerPass@123", SubscriptionTier = SubscriptionTier.Free,
        });
        tenantResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var tenant = await tenantResp.Content.ReadFromJsonAsync<CreateTenantResult>();
        var owner = factory.CreateMarinaOwnerClient(tenant!.TenantId);

        var marinaResp = await owner.PostAsJsonAsync("/marinas", new
        {
            Name = "Invoice Marina", Address = new { Street = "1 Marina St", City = "Port", State = "FL", Zip = "33000", Country = "US" },
            PhoneNumber = "555-0001", Email = "marina@inv.io", TimeZoneId = "America/New_York",
            Website = (string?)null, Description = (string?)null,
        });
        marinaResp.StatusCode.Should().Be(HttpStatusCode.Created);

        var custResp = await owner.PostAsJsonAsync("/customers", new
        {
            DisplayName = "Test Customer",
            BillingEmail = $"{Guid.NewGuid():N}@cust.io",
            BillingPhone = (string?)null,
            BillingAddress = (object?)null,
            EmergencyContactName = (string?)null,
            EmergencyContactPhone = (string?)null,
            Notes = (string?)null,
        });
        custResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var custId = await custResp.Content.ReadFromJsonAsync<Guid>();

        return (owner, custId);
    }

    // ── Create & query ────────────────────────────────────────────────────────

    [Fact]
    public async Task Create_invoice_returns_201_and_appears_in_list()
    {
        var (owner, custId) = await SetupAsync();

        var resp = await owner.PostAsJsonAsync("/invoices", new
        {
            CustomerAccountId = custId,
            IssuedDate = "2026-05-01",
            DueDate = "2026-05-31",
            Notes = (string?)null,
        });

        resp.StatusCode.Should().Be(HttpStatusCode.Created);
        var invoiceId = await resp.Content.ReadFromJsonAsync<Guid>();

        var list = await owner.GetFromJsonAsync<IReadOnlyList<InvoiceDto>>(
            $"/invoices?customerAccountId={custId}");
        list.Should().ContainSingle(i => i.Id == invoiceId && i.Status == InvoiceStatus.Draft);
    }

    [Fact]
    public async Task Get_invoice_by_id_returns_detail()
    {
        var (owner, custId) = await SetupAsync();

        var createResp = await owner.PostAsJsonAsync("/invoices", new
        {
            CustomerAccountId = custId,
            IssuedDate = "2026-05-01",
            DueDate = "2026-05-31",
            Notes = "Test notes",
        });
        var invoiceId = await createResp.Content.ReadFromJsonAsync<Guid>();

        var detail = await owner.GetFromJsonAsync<InvoiceDetailDto>($"/invoices/{invoiceId}");

        detail.Should().NotBeNull();
        detail!.Id.Should().Be(invoiceId);
        detail.Status.Should().Be(InvoiceStatus.Draft);
        detail.Notes.Should().Be("Test notes");
        detail.LineItems.Should().BeEmpty();
        detail.Payments.Should().BeEmpty();
    }

    [Fact]
    public async Task Get_unknown_invoice_returns_404()
    {
        var (owner, _) = await SetupAsync();
        var resp = await owner.GetAsync($"/invoices/{Guid.NewGuid()}");
        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── Line items ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Add_line_item_updates_totals()
    {
        var (owner, custId) = await SetupAsync();

        var createResp = await owner.PostAsJsonAsync("/invoices", new
        {
            CustomerAccountId = custId, IssuedDate = "2026-06-01", DueDate = "2026-06-30", Notes = (string?)null,
        });
        var invoiceId = await createResp.Content.ReadFromJsonAsync<Guid>();

        var lineResp = await owner.PostAsJsonAsync($"/invoices/{invoiceId}/line-items", new
        {
            Description = "Monthly slip fee – B-01",
            Quantity = 1m,
            UnitPrice = 650m,
            SlipAssignmentId = (Guid?)null,
        });
        lineResp.StatusCode.Should().Be(HttpStatusCode.Created);

        var detail = await owner.GetFromJsonAsync<InvoiceDetailDto>($"/invoices/{invoiceId}");
        detail!.LineItems.Should().ContainSingle();
        detail.SubTotal.Should().Be(650m);
        detail.TotalAmount.Should().Be(650m);
        detail.BalanceDue.Should().Be(650m);
    }

    [Fact]
    public async Task Update_line_item_recalculates_totals()
    {
        var (owner, custId) = await SetupAsync();

        var createResp = await owner.PostAsJsonAsync("/invoices", new
        {
            CustomerAccountId = custId, IssuedDate = "2026-06-01", DueDate = "2026-06-30", Notes = (string?)null,
        });
        var invoiceId = await createResp.Content.ReadFromJsonAsync<Guid>();

        var lineResp = await owner.PostAsJsonAsync($"/invoices/{invoiceId}/line-items", new
        {
            Description = "Slip fee", Quantity = 1m, UnitPrice = 600m, SlipAssignmentId = (Guid?)null,
        });
        var lineItemId = await lineResp.Content.ReadFromJsonAsync<Guid>();

        var updateResp = await owner.PutAsJsonAsync($"/invoices/{invoiceId}/line-items/{lineItemId}", new
        {
            Description = "Slip fee (corrected)", Quantity = 1m, UnitPrice = 700m,
        });
        updateResp.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var detail = await owner.GetFromJsonAsync<InvoiceDetailDto>($"/invoices/{invoiceId}");
        detail!.SubTotal.Should().Be(700m);
        detail.LineItems.Should().ContainSingle(li => li.Description == "Slip fee (corrected)");
    }

    [Fact]
    public async Task Remove_line_item_recalculates_totals()
    {
        var (owner, custId) = await SetupAsync();

        var createResp = await owner.PostAsJsonAsync("/invoices", new
        {
            CustomerAccountId = custId, IssuedDate = "2026-06-01", DueDate = "2026-06-30", Notes = (string?)null,
        });
        var invoiceId = await createResp.Content.ReadFromJsonAsync<Guid>();

        var line1Resp = await owner.PostAsJsonAsync($"/invoices/{invoiceId}/line-items", new
        {
            Description = "Item 1", Quantity = 1m, UnitPrice = 300m, SlipAssignmentId = (Guid?)null,
        });
        var line1Id = await line1Resp.Content.ReadFromJsonAsync<Guid>();

        await owner.PostAsJsonAsync($"/invoices/{invoiceId}/line-items", new
        {
            Description = "Item 2", Quantity = 1m, UnitPrice = 150m, SlipAssignmentId = (Guid?)null,
        });

        var deleteResp = await owner.DeleteAsync($"/invoices/{invoiceId}/line-items/{line1Id}");
        deleteResp.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var detail = await owner.GetFromJsonAsync<InvoiceDetailDto>($"/invoices/{invoiceId}");
        detail!.LineItems.Should().ContainSingle(li => li.Description == "Item 2");
        detail.SubTotal.Should().Be(150m);
    }

    [Fact]
    public async Task Cannot_add_line_item_to_sent_invoice()
    {
        var (owner, custId) = await SetupAsync();

        var createResp = await owner.PostAsJsonAsync("/invoices", new
        {
            CustomerAccountId = custId, IssuedDate = "2026-07-01", DueDate = "2026-07-31", Notes = (string?)null,
        });
        var invoiceId = await createResp.Content.ReadFromJsonAsync<Guid>();

        await owner.PostAsync($"/invoices/{invoiceId}/send", null);

        var lineResp = await owner.PostAsJsonAsync($"/invoices/{invoiceId}/line-items", new
        {
            Description = "Late add", Quantity = 1m, UnitPrice = 100m, SlipAssignmentId = (Guid?)null,
        });
        lineResp.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    // ── Status transitions ────────────────────────────────────────────────────

    [Fact]
    public async Task Send_invoice_transitions_to_sent()
    {
        var (owner, custId) = await SetupAsync();

        var createResp = await owner.PostAsJsonAsync("/invoices", new
        {
            CustomerAccountId = custId, IssuedDate = "2026-08-01", DueDate = "2026-08-31", Notes = (string?)null,
        });
        var invoiceId = await createResp.Content.ReadFromJsonAsync<Guid>();

        var sendResp = await owner.PostAsync($"/invoices/{invoiceId}/send", null);
        sendResp.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var detail = await owner.GetFromJsonAsync<InvoiceDetailDto>($"/invoices/{invoiceId}");
        detail!.Status.Should().Be(InvoiceStatus.Sent);
    }

    [Fact]
    public async Task Cannot_send_already_sent_invoice()
    {
        var (owner, custId) = await SetupAsync();

        var createResp = await owner.PostAsJsonAsync("/invoices", new
        {
            CustomerAccountId = custId, IssuedDate = "2026-09-01", DueDate = "2026-09-30", Notes = (string?)null,
        });
        var invoiceId = await createResp.Content.ReadFromJsonAsync<Guid>();

        await owner.PostAsync($"/invoices/{invoiceId}/send", null);
        var secondSend = await owner.PostAsync($"/invoices/{invoiceId}/send", null);

        secondSend.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Void_draft_invoice()
    {
        var (owner, custId) = await SetupAsync();

        var createResp = await owner.PostAsJsonAsync("/invoices", new
        {
            CustomerAccountId = custId, IssuedDate = "2026-10-01", DueDate = "2026-10-31", Notes = (string?)null,
        });
        var invoiceId = await createResp.Content.ReadFromJsonAsync<Guid>();

        var voidResp = await owner.PostAsync($"/invoices/{invoiceId}/void", null);
        voidResp.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var detail = await owner.GetFromJsonAsync<InvoiceDetailDto>($"/invoices/{invoiceId}");
        detail!.Status.Should().Be(InvoiceStatus.Voided);
    }

    [Fact]
    public async Task Void_sent_invoice()
    {
        var (owner, custId) = await SetupAsync();

        var createResp = await owner.PostAsJsonAsync("/invoices", new
        {
            CustomerAccountId = custId, IssuedDate = "2026-11-01", DueDate = "2026-11-30", Notes = (string?)null,
        });
        var invoiceId = await createResp.Content.ReadFromJsonAsync<Guid>();

        await owner.PostAsync($"/invoices/{invoiceId}/send", null);

        var voidResp = await owner.PostAsync($"/invoices/{invoiceId}/void", null);
        voidResp.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var detail = await owner.GetFromJsonAsync<InvoiceDetailDto>($"/invoices/{invoiceId}");
        detail!.Status.Should().Be(InvoiceStatus.Voided);
    }

    // ── Payments ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Partial_payment_sets_status_to_partially_paid()
    {
        var (owner, custId) = await SetupAsync();
        var invoiceId = await CreateSentInvoiceWithAmountAsync(owner, custId, 500m, "2026-12-01", "2026-12-31");

        var payResp = await owner.PostAsJsonAsync($"/invoices/{invoiceId}/payments", new
        {
            Amount = 200m,
            PaidOn = "2026-12-10",
            Method = PaymentMethod.Cash,
            ReferenceNumber = (string?)null,
            Notes = (string?)null,
        });
        payResp.StatusCode.Should().Be(HttpStatusCode.Created);

        var detail = await owner.GetFromJsonAsync<InvoiceDetailDto>($"/invoices/{invoiceId}");
        detail!.Status.Should().Be(InvoiceStatus.PartiallyPaid);
        detail.AmountPaid.Should().Be(200m);
        detail.BalanceDue.Should().Be(300m);
        detail.Payments.Should().ContainSingle(p => p.Amount == 200m);
    }

    [Fact]
    public async Task Full_payment_sets_status_to_paid()
    {
        var (owner, custId) = await SetupAsync();
        var invoiceId = await CreateSentInvoiceWithAmountAsync(owner, custId, 400m, "2027-01-01", "2027-01-31");

        var payResp = await owner.PostAsJsonAsync($"/invoices/{invoiceId}/payments", new
        {
            Amount = 400m,
            PaidOn = "2027-01-15",
            Method = PaymentMethod.Check,
            ReferenceNumber = "CHK-1001",
            Notes = (string?)null,
        });
        payResp.StatusCode.Should().Be(HttpStatusCode.Created);

        var detail = await owner.GetFromJsonAsync<InvoiceDetailDto>($"/invoices/{invoiceId}");
        detail!.Status.Should().Be(InvoiceStatus.Paid);
        detail.AmountPaid.Should().Be(400m);
        detail.BalanceDue.Should().Be(0m);
    }

    [Fact]
    public async Task Two_partial_payments_sum_to_paid()
    {
        var (owner, custId) = await SetupAsync();
        var invoiceId = await CreateSentInvoiceWithAmountAsync(owner, custId, 600m, "2027-02-01", "2027-02-28");

        await owner.PostAsJsonAsync($"/invoices/{invoiceId}/payments", new
        {
            Amount = 350m, PaidOn = "2027-02-05",
            Method = PaymentMethod.Cash, ReferenceNumber = (string?)null, Notes = (string?)null,
        });

        await owner.PostAsJsonAsync($"/invoices/{invoiceId}/payments", new
        {
            Amount = 250m, PaidOn = "2027-02-20",
            Method = PaymentMethod.Check, ReferenceNumber = "CHK-999", Notes = (string?)null,
        });

        var detail = await owner.GetFromJsonAsync<InvoiceDetailDto>($"/invoices/{invoiceId}");
        detail!.Status.Should().Be(InvoiceStatus.Paid);
        detail.Payments.Should().HaveCount(2);
        detail.AmountPaid.Should().Be(600m);
    }

    [Fact]
    public async Task Payment_exceeding_balance_returns_400()
    {
        var (owner, custId) = await SetupAsync();
        var invoiceId = await CreateSentInvoiceWithAmountAsync(owner, custId, 300m, "2027-03-01", "2027-03-31");

        var payResp = await owner.PostAsJsonAsync($"/invoices/{invoiceId}/payments", new
        {
            Amount = 999m, PaidOn = "2027-03-10",
            Method = PaymentMethod.Cash, ReferenceNumber = (string?)null, Notes = (string?)null,
        });
        payResp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Cannot_record_payment_on_draft_invoice()
    {
        var (owner, custId) = await SetupAsync();

        var createResp = await owner.PostAsJsonAsync("/invoices", new
        {
            CustomerAccountId = custId, IssuedDate = "2027-04-01", DueDate = "2027-04-30", Notes = (string?)null,
        });
        var invoiceId = await createResp.Content.ReadFromJsonAsync<Guid>();

        var payResp = await owner.PostAsJsonAsync($"/invoices/{invoiceId}/payments", new
        {
            Amount = 100m, PaidOn = "2027-04-05",
            Method = PaymentMethod.Cash, ReferenceNumber = (string?)null, Notes = (string?)null,
        });
        payResp.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Cannot_void_paid_invoice()
    {
        var (owner, custId) = await SetupAsync();
        var invoiceId = await CreateSentInvoiceWithAmountAsync(owner, custId, 100m, "2027-05-01", "2027-05-31");

        await owner.PostAsJsonAsync($"/invoices/{invoiceId}/payments", new
        {
            Amount = 100m, PaidOn = "2027-05-10",
            Method = PaymentMethod.Cash, ReferenceNumber = (string?)null, Notes = (string?)null,
        });

        var voidResp = await owner.PostAsync($"/invoices/{invoiceId}/void", null);
        voidResp.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    // ── Filters ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Filter_by_status_returns_only_matching_invoices()
    {
        var (owner, custId) = await SetupAsync();

        // Create two draft invoices
        var resp1 = await owner.PostAsJsonAsync("/invoices", new
        {
            CustomerAccountId = custId, IssuedDate = "2027-06-01", DueDate = "2027-06-30", Notes = (string?)null,
        });
        var id1 = await resp1.Content.ReadFromJsonAsync<Guid>();

        var resp2 = await owner.PostAsJsonAsync("/invoices", new
        {
            CustomerAccountId = custId, IssuedDate = "2027-07-01", DueDate = "2027-07-31", Notes = (string?)null,
        });
        var id2 = await resp2.Content.ReadFromJsonAsync<Guid>();

        // Send only the first
        await owner.PostAsync($"/invoices/{id1}/send", null);

        var drafts = await owner.GetFromJsonAsync<IReadOnlyList<InvoiceDto>>(
            $"/invoices?customerAccountId={custId}&status={InvoiceStatus.Draft}");
        var sent   = await owner.GetFromJsonAsync<IReadOnlyList<InvoiceDto>>(
            $"/invoices?customerAccountId={custId}&status={InvoiceStatus.Sent}");

        drafts.Should().Contain(i => i.Id == id2);
        drafts.Should().NotContain(i => i.Id == id1);
        sent.Should().Contain(i => i.Id == id1);
        sent.Should().NotContain(i => i.Id == id2);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Creates a Draft invoice, adds a single line item for the given amount, then sends it.
    /// Returns the invoice ID ready for payment tests.
    /// </summary>
    private async Task<Guid> CreateSentInvoiceWithAmountAsync(
        HttpClient owner, Guid custId, decimal amount, string issuedDate, string dueDate)
    {
        var createResp = await owner.PostAsJsonAsync("/invoices", new
        {
            CustomerAccountId = custId,
            IssuedDate = issuedDate,
            DueDate = dueDate,
            Notes = (string?)null,
        });
        var invoiceId = await createResp.Content.ReadFromJsonAsync<Guid>();

        await owner.PostAsJsonAsync($"/invoices/{invoiceId}/line-items", new
        {
            Description = "Slip fee",
            Quantity = 1m,
            UnitPrice = amount,
            SlipAssignmentId = (Guid?)null,
        });

        await owner.PostAsync($"/invoices/{invoiceId}/send", null);

        return invoiceId;
    }
}

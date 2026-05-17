// Private slip owner (3 frames) + Platform operator (3 frames) + Cross-cutting (3 frames)

// ── Private slip owner ─────────────────────────────────────────
function FramePrivateOnboard() {
  return (
    <WFPage>
      <WFPersonaRibbon persona="Private slip owner" scenario="Onboarding · 'Add my dock'" />
      <WFAppBar active="My slips" persona="Free tier" />
      <div style={{ padding: '20px 30px', display: 'grid', gridTemplateColumns: '1fr 280px', gap: 24, height: 'calc(100% - 40px)' }}>
        <div style={{ overflow: 'hidden' }}>
          <WFLabel>Step 2 of 4</WFLabel>
          <WFTitle level={1}>Where is your dock?</WFTitle>
          <WFNote>We'll set up your slip listing in under 2 minutes.</WFNote>
          <div style={{ display: 'flex', gap: 8, marginTop: 16 }}>
            <WFCard accent style={{ flex: 1 }}>
              <div style={{ fontFamily: wfFontMono, fontSize: 18, fontWeight: 700 }}>1</div>
              <div style={{ fontWeight: 600, marginTop: 4 }}>It's at my home</div>
              <WFNote style={{ fontSize: 11 }}>Private dock on my own waterfront</WFNote>
              <WFTag style={{ marginTop: 6 }}>SELECTED</WFTag>
            </WFCard>
            <WFCard style={{ flex: 1 }}>
              <div style={{ fontFamily: wfFontMono, fontSize: 18, fontWeight: 700 }}>2</div>
              <div style={{ fontWeight: 600, marginTop: 4 }}>I own a slip at a marina</div>
              <WFNote style={{ fontSize: 11 }}>Dockominium · the marina hosts it physically</WFNote>
            </WFCard>
          </div>
          <div style={{ marginTop: 16, display: 'flex', flexDirection: 'column', gap: 8 }}>
            <WFInput label="Property address" value="412 Bayside Lane, Solomons MD 20688" big />
            <WFInput label="Name your dock (boaters see this)" value="Pat's dock at Solomons" big />
          </div>
          <WFMap style={{ marginTop: 12, height: 100 }} pins={1} label="map · drop pin to refine" />
          <div style={{ display: 'flex', justifyContent: 'space-between', marginTop: 14 }}>
            <WFButton>← Back</WFButton>
            <WFButton primary>Continue → slip details</WFButton>
          </div>
        </div>
        {/* Right: progress + bookkeeping callout */}
        <div>
          <WFCard>
            <WFLabel>Setup</WFLabel>
            <div style={{ marginTop: 8, fontSize: 12, lineHeight: 1.8 }}>
              <div>✓ &nbsp;Account &amp; email</div>
              <div style={{ background: WF_HIGHLIGHT, padding: '0 4px', display: 'inline-block' }}>● &nbsp;Where</div>
              <div>○ &nbsp;Slip details (size, type)</div>
              <div>○ &nbsp;Photos (1–4)</div>
              <div>○ &nbsp;Pricing &amp; first listing</div>
            </div>
          </WFCard>
          <WFAnnotation rotate={-3} style={{ position: 'static', marginTop: 16, color: WF_INK_SOFT, fontSize: 12 }}>
            ⚙ behind the scenes:<br/>
            • Free-tier Tenant<br/>
            • single-slip Marina (PrivateDock)<br/>
            • Slip · HostMarinaId = null<br/>
            • Owner Membership<br/>
            <br/>UX never says "marina"
          </WFAnnotation>
        </div>
      </div>
    </WFPage>
  );
}

function FramePrivateDashboard() {
  return (
    <WFPage>
      <WFPersonaRibbon persona="Private slip owner" scenario="Single-slip dashboard" />
      <WFAppBar active="My slips" />
      <div style={{ padding: 16, display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 14, height: 'calc(100% - 40px)' }}>
        <div>
          <WFTitle level={1}>Pat's dock</WFTitle>
          <WFNote>Solomons, MD · 1 slip · Free tier</WFNote>
          <WFCard style={{ marginTop: 10 }}>
            <div style={{ display: 'flex', gap: 10 }}>
              <WFPlaceholder label="dock photo" height={70} style={{ width: 110 }} />
              <div style={{ flex: 1 }}>
                <div style={{ fontWeight: 600 }}>Main slip · 38' max</div>
                <WFNote style={{ fontSize: 11 }}>Floating · 30A electric · water · fresh paint</WFNote>
                <div style={{ display: 'flex', gap: 5, marginTop: 5 }}>
                  <WFTag>Listed</WFTag><WFTag style={{ background: WF_PAPER_LINE }}>Instant book</WFTag>
                </div>
              </div>
            </div>
          </WFCard>
          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr 1fr', gap: 8, marginTop: 12 }}>
            {[['This month','$340','3 res'],['YTD','$1,820','12 res'],['Avg/night','$95','--']].map(c => (
              <WFCard key={c[0]}>
                <WFLabel>{c[0]}</WFLabel>
                <div style={{ fontFamily: wfFontMono, fontSize: 18, marginTop: 4 }}>{c[1]}</div>
                <WFNote style={{ fontSize: 10 }}>{c[2]}</WFNote>
              </WFCard>
            ))}
          </div>
          <WFCard style={{ marginTop: 10 }}>
            <WFLabel>Reservations</WFLabel>
            <div style={{ marginTop: 5, fontSize: 11, display: 'flex', flexDirection: 'column', gap: 4 }}>
              <div style={{ display: 'flex', justifyContent: 'space-between' }}>· Aug 5–7 · T. Reyes <WFTag>Confirmed</WFTag></div>
              <div style={{ display: 'flex', justifyContent: 'space-between' }}>· Aug 10–11 · J. Lin <WFTag style={{ background: WF_PAPER_LINE }}>Pending</WFTag></div>
              <div style={{ display: 'flex', justifyContent: 'space-between' }}>· Aug 22–28 · M. Park <WFTag>Confirmed</WFTag></div>
            </div>
          </WFCard>
          <WFCard style={{ marginTop: 10, background: WF_HIGHLIGHT }}>
            <WFLabel>Era 1 reminder</WFLabel>
            <WFNote style={{ fontSize: 11, marginTop: 4 }}>Boaters pay you directly. We'll email you a printable invoice for each booking. Online payouts launch with Stripe Connect.</WFNote>
          </WFCard>
        </div>
        <div>
          <WFTitle level={2}>Calendar &amp; pricing</WFTitle>
          <WFCard style={{ marginTop: 6 }}>
            <div style={{ display: 'grid', gridTemplateColumns: 'repeat(7,1fr)', gap: 2, marginTop: 4 }}>
              {Array.from({ length: 21 }).map((_, i) => {
                const booked = [4,5,6,9,10,21].includes(i);
                const blocked = [13].includes(i);
                return (
                  <div key={i} style={{ height: 30, border: `1px solid ${WF_INK_FAINT}`, background: booked ? WF_INK : blocked ? WF_PAPER_LINE : '#fff', color: booked ? '#fff' : WF_INK, fontSize: 9, padding: 2, fontFamily: wfFontMono }}>
                    {i + 1}
                    {booked && <div style={{ fontSize: 8 }}>$95</div>}
                  </div>
                );
              })}
            </div>
            <WFNote style={{ fontSize: 10, marginTop: 6 }}>Drag to add window · click to block out</WFNote>
          </WFCard>
          <WFCard style={{ marginTop: 10 }}>
            <WFLabel>Quick price</WFLabel>
            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 6, marginTop: 6 }}>
              <WFInput label="Per night" value="$95" />
              <WFInput label="Cleaning" value="$0" />
            </div>
            <div style={{ display: 'flex', justifyContent: 'space-between', marginTop: 8, fontSize: 12 }}><span>Instant book</span><WFToggle on /></div>
            <div style={{ display: 'flex', justifyContent: 'space-between', marginTop: 4, fontSize: 12 }}><span>Visible in search</span><WFToggle on /></div>
          </WFCard>
        </div>
      </div>
    </WFPage>
  );
}

function FramePrivateDockominium() {
  return (
    <WFPage>
      <WFPersonaRibbon persona="Dockominium owner" scenario="Host-marina policy" />
      <WFAppBar active="My slips" />
      <div style={{ padding: 16, display: 'grid', gridTemplateColumns: '1.2fr 1fr', gap: 14, height: 'calc(100% - 40px)' }}>
        <div>
          <WFTitle level={1}>Maria's slip at Big Bay</WFTitle>
          <WFNote>Slip A-12 · physically located at Big Bay Marina (host) · you own it outright</WFNote>
          <WFCard accent style={{ marginTop: 10 }}>
            <div style={{ display: 'flex', justifyContent: 'space-between' }}>
              <div>
                <WFLabel>Host marina policy</WFLabel>
                <WFTitle level={2} style={{ marginTop: 2 }}>NotifyOnly</WFTitle>
                <WFNote style={{ fontSize: 11 }}>Big Bay sees your bookings (security/ops). They cannot decline.</WFNote>
              </div>
              <WFButton small>Change</WFButton>
            </div>
            <WFLine dashed style={{ margin: '10px 0' }} />
            <div style={{ display: 'flex', flexDirection: 'column', gap: 6 }}>
              {[
                ['None', 'Bypass Big Bay entirely. They see nothing.'],
                ['NotifyOnly', 'Big Bay sees bookings (informational).', true],
                ['RequiresApproval', 'Big Bay must approve every booking before it confirms.'],
              ].map(([t, d, sel]) => (
                <div key={t} style={{ display: 'flex', gap: 8, padding: 6, background: sel ? WF_HIGHLIGHT : 'transparent', border: `1px ${sel ? 'solid' : 'dashed'} ${sel ? WF_INK : WF_INK_FAINT}`, fontSize: 11 }}>
                  <span style={{ width: 11, height: 11, borderRadius: 99, border: `1.5px solid ${WF_INK}`, background: sel ? WF_INK : 'transparent', marginTop: 2 }} />
                  <div><strong>{t}</strong> · <span style={{ color: WF_INK_SOFT }}>{d}</span></div>
                </div>
              ))}
            </div>
          </WFCard>
          <WFCard style={{ marginTop: 10 }}>
            <WFLabel>Host-marina fee deduction</WFLabel>
            <WFNote style={{ fontSize: 11, marginTop: 4 }}>$15/booking · gate-access provisioning · paid out to Big Bay (RevenueSplit · payeeKind = HostMarina)</WFNote>
          </WFCard>
        </div>
        {/* Right: cross-role panel */}
        <div>
          <WFTitle level={2}>You also rent from Big Bay</WFTitle>
          <WFNote>Same login · no toggle</WFNote>
          <WFCard style={{ marginTop: 8 }}>
            <WFLabel>Your customer relationship at Big Bay</WFLabel>
            <div style={{ marginTop: 6, fontSize: 12, lineHeight: 1.7 }}>
              · Billing account: <strong>Maria Rodriguez</strong><br />
              · HOA fees · $320/mo<br />
              · Pump-out service (a la carte)<br />
              · 2 announcements unread
            </div>
            <WFButton small style={{ marginTop: 8 }}>Open customer view →</WFButton>
          </WFCard>
          <WFAnnotation rotate={2} color={WF_BAD} style={{ position: 'static', marginTop: 14 }}>
            ★ Two roles, same physical site, one dashboard.
          </WFAnnotation>
        </div>
      </div>
    </WFPage>
  );
}

// ── Platform operator (3 frames) ────────────────────────────
const PLAT_NAV = ['Tenants','Users','Listings','Reservations','Audit log','Demo tenants','Health'];

function FramePlatformTenants() {
  return (
    <WFPage>
      <WFPersonaRibbon persona="Platform Operator" scenario="Tenant management" />
      <div style={{ display: 'flex', height: '100%' }}>
        <WFSideRail active="Tenants" items={PLAT_NAV} title="MyMarina · Console" />
        <div style={{ flex: 1, padding: 16 }}>
          <div style={{ display: 'flex', justifyContent: 'space-between' }}>
            <WFTitle level={1}>Tenants</WFTitle>
            <WFButton small primary>+ Provision</WFButton>
          </div>
          <div style={{ display: 'flex', gap: 6, marginTop: 8, flexWrap: 'wrap' }}>
            <WFPill bg={WF_HIGHLIGHT}>All 247</WFPill>
            <WFPill>Commercial 84</WFPill>
            <WFPill>Yacht clubs 12</WFPill>
            <WFPill>Private hosts 142</WFPill>
            <WFPill>Demo 3</WFPill>
            <WFPill color={WF_BAD}>Suspended 6</WFPill>
            <div style={{ flex: 1 }} />
            <WFInput value="Search by name, slug, owner email" style={{ width: 240 }} />
          </div>
          <WFCard padding={0} style={{ marginTop: 10 }}>
            <div style={{ display: 'grid', gridTemplateColumns: '1.4fr 90px 80px 80px 90px 80px 70px', padding: '6px 10px', fontFamily: wfFontMono, fontSize: 9, textTransform: 'uppercase', color: WF_INK_SOFT, borderBottom: `1px dashed ${WF_INK_FAINT}` }}>
              <span>Tenant</span><span>Type</span><span>Tier</span><span>Marinas</span><span>Slips</span><span>Status</span><span></span>
            </div>
            {[
              ['Big Bay Marina LLC','Commercial','Pro','1','110','Active'],
              ['Eastport Yacht Club','YachtClub','Premium','1','78','Active'],
              ['Pat Sweeney','PrivateDock','Free','1','1','Active'],
              ['Maria Rodriguez','Dockominium','Free','1','1','Active'],
              ['Tidewater Group','Commercial','Pro','3','340','Active'],
              ['Demo · Sample Marina','Demo','Pro','1','40','Active'],
              ['Old Anchor (deactivated)','Commercial','Free','1','22','Suspended'],
            ].map((r, i) => (
              <div key={r[0]} style={{ display: 'grid', gridTemplateColumns: '1.4fr 90px 80px 80px 90px 80px 70px', padding: '8px 10px', borderBottom: i<6 ? `1px dashed ${WF_INK_FAINT}` : 'none', fontSize: 11, alignItems: 'center', background: r[5]==='Suspended' ? WF_PAPER_LINE : undefined }}>
                <span style={{ fontWeight: 600 }}>{r[0]}</span>
                <WFTag style={{ background: r[1]==='Demo' ? WF_HIGHLIGHT : WF_PAPER_LINE }}>{r[1]}</WFTag>
                <span style={{ fontFamily: wfFontMono, fontSize: 10 }}>{r[2]}</span>
                <span style={{ fontFamily: wfFontMono }}>{r[3]}</span>
                <span style={{ fontFamily: wfFontMono }}>{r[4]}</span>
                <WFTag style={{ background: r[5]==='Suspended' ? '#ffd' : 'transparent' }}>{r[5]}</WFTag>
                <WFButton small>Open</WFButton>
              </div>
            ))}
          </WFCard>
          <WFAnnotation style={{ bottom: 30, right: 30 }}>
            all actions → AuditLog<br/>(platform_action flag)
          </WFAnnotation>
        </div>
      </div>
    </WFPage>
  );
}

function FramePlatformModeration() {
  return (
    <WFPage>
      <WFPersonaRibbon persona="Platform Operator" scenario="Listing moderation queue" />
      <div style={{ display: 'flex', height: '100%' }}>
        <WFSideRail active="Listings" items={PLAT_NAV} title="MyMarina · Console" />
        <div style={{ flex: 1, padding: 16, display: 'grid', gridTemplateColumns: '1fr 280px', gap: 12 }}>
          <div>
            <WFTitle level={1}>Reported listings</WFTitle>
            <WFNote>4 open · keep the marketplace healthy</WFNote>
            <div style={{ marginTop: 10, display: 'flex', flexDirection: 'column', gap: 8 }}>
              {[
                { t: "Pat's dock at Solomons", reason:'Misleading photos', count:3, focus:true },
                { t:"Maria's slip · Big Bay", reason:'Price gouging', count:1 },
                { t:'Tidewater · 3 listings', reason:'Duplicate amenities', count:2 },
              ].map((r,i) => (
                <WFCard key={i} accent={r.focus}>
                  <div style={{ display: 'flex', justifyContent: 'space-between' }}>
                    <div>
                      <strong>{r.t}</strong>
                      <WFNote style={{ fontSize: 11, marginTop: 2 }}>Reason: {r.reason} · {r.count} report{r.count>1?'s':''}</WFNote>
                    </div>
                    <WFPill color={WF_BAD}>Open</WFPill>
                  </div>
                  {r.focus && <div style={{ display: 'flex', gap: 5, marginTop: 8 }}>
                    <WFButton small>View listing</WFButton>
                    <WFButton small>Take down</WFButton>
                    <WFButton small>Dismiss</WFButton>
                  </div>}
                </WFCard>
              ))}
            </div>
          </div>
          <WFCard accent>
            <WFLabel>Selected · Pat's dock</WFLabel>
            <WFTitle level={2}>3 reports · 2 reasons</WFTitle>
            <div style={{ marginTop: 6, fontSize: 11 }}>
              · "Photos look photoshopped"<br />
              · "Photos · pier looks unsafe"<br />
              · "Photos don't match"
            </div>
            <WFLine dashed style={{ margin: '10px 0' }} />
            <WFLabel>Listing audit</WFLabel>
            <div style={{ fontFamily: wfFontMono, fontSize: 9, marginTop: 4, lineHeight: 1.6 }}>
              created 2026-04-12 by pat@…<br/>
              photos updated 2026-07-01<br/>
              5 windows · 3 confirmed res.<br/>
              0 cancellations
            </div>
            <WFLine dashed style={{ margin: '10px 0' }} />
            <div style={{ display: 'flex', flexDirection: 'column', gap: 5 }}>
              <WFButton primary small>Take down + email host</WFButton>
              <WFButton small>Photo-only takedown</WFButton>
              <WFButton small>Reinstate</WFButton>
              <WFButton small>Disable host</WFButton>
            </div>
            <WFNote style={{ fontSize: 10, marginTop: 8, color: WF_INK_SOFT }}>All actions write to AuditLog with platform_action flag.</WFNote>
          </WFCard>
        </div>
      </div>
    </WFPage>
  );
}

function FramePlatformUser() {
  return (
    <WFPage>
      <WFPersonaRibbon persona="Platform Operator" scenario="User detail" />
      <div style={{ display: 'flex', height: '100%' }}>
        <WFSideRail active="Users" items={PLAT_NAV} title="MyMarina · Console" />
        <div style={{ flex: 1, padding: 16, overflow: 'hidden' }}>
          <WFNote>← Users / search</WFNote>
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'baseline' }}>
            <div>
              <WFTitle level={1}>Maria Rodriguez</WFTitle>
              <WFNote>maria.r@email.com · joined 2025-03 · 38 logins · last 2026-08-04</WFNote>
            </div>
            <div style={{ display: 'flex', gap: 6 }}>
              <WFButton small>Reset pw</WFButton>
              <WFButton small>Revoke tokens</WFButton>
              <WFButton small>Disable</WFButton>
            </div>
          </div>
          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr 1fr', gap: 12, marginTop: 12 }}>
            <WFCard>
              <WFLabel>Memberships (host-side)</WFLabel>
              <div style={{ fontSize: 11, marginTop: 6, lineHeight: 1.7 }}>
                · Owner · "Maria's slip at Big Bay" (Dockominium)<br />
                <span style={{ color: WF_INK_FAINT }}>since 2025-03 · 1 slip</span>
              </div>
            </WFCard>
            <WFCard>
              <WFLabel>BillingAccount memberships</WFLabel>
              <div style={{ fontSize: 11, marginTop: 6, lineHeight: 1.7 }}>
                · Big Bay Marina · Owner<br />
                · Eastport YC · Member<br />
                <span style={{ color: WF_INK_FAINT }}>customer-side at 2 marinas</span>
              </div>
            </WFCard>
            <WFCard>
              <WFLabel>Vessels</WFLabel>
              <div style={{ fontSize: 11, marginTop: 6, lineHeight: 1.7 }}>
                · Halcyon · Beneteau 44'<br />
                · Tender · 12'<br />
                <span style={{ color: WF_INK_FAINT }}>both owner-claimed</span>
              </div>
            </WFCard>
          </div>
          <div style={{ display: 'grid', gridTemplateColumns: '1.4fr 1fr', gap: 12, marginTop: 12 }}>
            <WFCard>
              <WFLabel>Reservation history (last 6)</WFLabel>
              <div style={{ marginTop: 6, fontSize: 11, fontFamily: wfFontMono, lineHeight: 1.7 }}>
                2026-08-05 · Big Bay A-12 · Confirmed<br />
                2026-07-12 · Pat's dock · Completed<br />
                2026-06-04 · Eastport Guest 3 · Completed<br />
                2026-05-22 · Tidewater B-14 · Cancelled (boater)<br />
                2026-05-01 · Big Bay A-12 · Completed<br />
                2026-04-15 · Pat's dock · Completed
              </div>
            </WFCard>
            <WFCard>
              <WFLabel>Recent audit (cross-tenant)</WFLabel>
              <div style={{ fontFamily: wfFontMono, fontSize: 9, marginTop: 6, lineHeight: 1.6 }}>
                2026-08-04 · login (Google)<br/>
                2026-08-03 · vessel.update Halcyon<br/>
                2026-07-28 · reservation.create #4821<br/>
                2026-07-12 · billing.invoice.pay $640<br/>
                2026-07-01 · listing.create A-12 window<br/>
              </div>
            </WFCard>
          </div>
        </div>
      </div>
    </WFPage>
  );
}

// ── Cross-cutting (3 frames) ───────────────────────────────
function FrameSignIn() {
  return (
    <WFPage>
      <WFPersonaRibbon persona="Anyone" scenario="Sign in / sign up" />
      <div style={{ height: '100%', display: 'flex', alignItems: 'center', justifyContent: 'center', padding: 30 }}>
        <div style={{ display: 'grid', gridTemplateColumns: '1fr 320px', gap: 30, alignItems: 'center', maxWidth: 700 }}>
          <div>
            <div style={{ fontFamily: wfFontHand, fontSize: 36, fontWeight: 700, lineHeight: 1 }}>⚓ MyMarina</div>
            <WFTitle level={1} style={{ marginTop: 14, fontSize: 24 }}>One account.<br/>Every marina.</WFTitle>
            <WFNote style={{ marginTop: 10, fontSize: 13 }}>
              Find slips, manage your boats, and run the marinas you host — all from one login. No per-marina sign-ups, no context switch.
            </WFNote>
            <div style={{ marginTop: 16, display: 'flex', gap: 10, flexWrap: 'wrap' }}>
              <WFTag>Boater</WFTag><WFTag>Marina operator</WFTag><WFTag>Private dock</WFTag><WFTag>Dockominium</WFTag>
            </div>
          </div>
          <WFCard accent>
            <WFTitle level={2}>Sign in</WFTitle>
            <div style={{ display: 'flex', flexDirection: 'column', gap: 8, marginTop: 10 }}>
              <WFButton style={{ display: 'flex', alignItems: 'center', justifyContent: 'center', gap: 6 }}>G · Continue with Google</WFButton>
              <WFButton style={{ display: 'flex', alignItems: 'center', justifyContent: 'center', gap: 6 }}> · Continue with Apple</WFButton>
              <WFButton style={{ display: 'flex', alignItems: 'center', justifyContent: 'center', gap: 6 }}>f · Continue with Facebook</WFButton>
              <div style={{ textAlign: 'center', fontFamily: wfFontMono, fontSize: 9, color: WF_INK_SOFT, margin: '4px 0' }}>— or —</div>
              <WFInput label="Email" value="you@email.com" />
              <WFInput label="Password" value="••••••••" />
              <WFButton primary>Sign in</WFButton>
              <div style={{ textAlign: 'center', fontSize: 11, marginTop: 6 }}>New here? <WFUnderline>Create an account</WFUnderline></div>
            </div>
          </WFCard>
        </div>
      </div>
    </WFPage>
  );
}

function FrameVesselClaim() {
  return (
    <WFPage>
      <WFPersonaRibbon persona="New boater" scenario="Ghost vessel claim · first sign-in" />
      <WFAppBar active="My boats" />
      <div style={{ padding: 20, display: 'flex', justifyContent: 'center' }}>
        <div style={{ width: 540 }}>
          <WFTitle level={1}>2 boats are waiting for you</WFTitle>
          <WFNote>Marinas you've worked with added these to track your slip and insurance. Confirm the ones that are yours — you can correct or reject any.</WFNote>
          <div style={{ display: 'flex', flexDirection: 'column', gap: 10, marginTop: 14 }}>
            <WFCard accent>
              <div style={{ display: 'flex', gap: 10 }}>
                <WFPlaceholder label="boat" height={64} style={{ width: 90 }} />
                <div style={{ flex: 1 }}>
                  <div style={{ fontWeight: 600 }}>Halcyon</div>
                  <WFNote style={{ fontSize: 11 }}>Beneteau 44 · 44'×14'×7' · added by Big Bay Marina</WFNote>
                  <WFTag style={{ marginTop: 4 }}>added 2025-03-12</WFTag>
                </div>
                <div style={{ display: 'flex', flexDirection: 'column', gap: 4 }}>
                  <WFButton small primary>That's mine</WFButton>
                  <WFButton small>Not mine</WFButton>
                  <WFButton small>Edit details</WFButton>
                </div>
              </div>
            </WFCard>
            <WFCard>
              <div style={{ display: 'flex', gap: 10 }}>
                <WFPlaceholder label="boat" height={64} style={{ width: 90 }} />
                <div style={{ flex: 1 }}>
                  <div style={{ fontWeight: 600 }}>Tender</div>
                  <WFNote style={{ fontSize: 11 }}>Achilles 12 · 12' · added by Eastport YC</WFNote>
                  <WFTag style={{ marginTop: 4 }}>added 2025-04-02</WFTag>
                </div>
                <div style={{ display: 'flex', flexDirection: 'column', gap: 4 }}>
                  <WFButton small primary>That's mine</WFButton>
                  <WFButton small>Not mine</WFButton>
                </div>
              </div>
            </WFCard>
          </div>
          <WFAnnotation rotate={-2} color={WF_BAD} style={{ position: 'static', marginTop: 14 }}>
            ↳ Vessel.OwnerUserId set on accept · ClaimedAt recorded · marina notified
          </WFAnnotation>
          <div style={{ display: 'flex', justifyContent: 'space-between', marginTop: 12 }}>
            <WFButton>Skip for now</WFButton>
            <WFButton primary>Continue → my boats</WFButton>
          </div>
        </div>
      </div>
    </WFPage>
  );
}

function FrameSubletDiagram() {
  return (
    <WFPage>
      <WFPersonaRibbon persona="System diagram" scenario="3 sources of marketplace availability" />
      <div style={{ padding: 20 }}>
        <WFTitle level={1}>Where listings come from</WFTitle>
        <WFNote>One slip · three possible listers. The differentiator vs. legacy marina software.</WFNote>
        <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr 1fr', gap: 14, marginTop: 18 }}>
          {/* 1. Owner-direct */}
          <WFCard>
            <WFLabel>1 · Owner-direct</WFLabel>
            <WFTitle level={2} style={{ marginTop: 4 }}>Marina lists their slip</WFTitle>
            <WFNote style={{ fontSize: 11, marginTop: 4 }}>The most common case. ListedByKind = Owner.</WFNote>
            <div style={{ marginTop: 10, fontFamily: wfFontMono, fontSize: 10, lineHeight: 1.6 }}>
              boater $$$ →<br />
              <strong>SlipOwner 95%</strong><br />
              Platform 5%
            </div>
          </WFCard>
          {/* 2. Holder sublet */}
          <WFCard accent>
            <WFLabel>2 · Holder sublet</WFLabel>
            <WFTitle level={2} style={{ marginTop: 4 }}>Lease holder lists it</WFTitle>
            <WFNote style={{ fontSize: 11, marginTop: 4 }}>"Going to Bermuda for a month." Subject to <code>AllowHolderSublet</code>. ListedByKind = Holder.</WFNote>
            <div style={{ marginTop: 10, fontFamily: wfFontMono, fontSize: 10, lineHeight: 1.6 }}>
              boater $$$ →<br />
              <strong>Holder 80%</strong><br />
              SlipOwner 15%<br />
              Platform 5%
            </div>
          </WFCard>
          {/* 3. Owner-for-holder */}
          <WFCard>
            <WFLabel>3 · Owner-sublet of leased slip</WFLabel>
            <WFTitle level={2} style={{ marginTop: 4 }}>Marina lists during holder's "I'm Away"</WFTitle>
            <WFNote style={{ fontSize: 11, marginTop: 4 }}>Holder marks away → marina sublets → revenue split back. ListedByKind = OwnerForHolder.</WFNote>
            <div style={{ marginTop: 10, fontFamily: wfFontMono, fontSize: 10, lineHeight: 1.6 }}>
              boater $$$ →<br />
              <strong>SlipOwner 65%</strong><br />
              Holder 30% (incentive!)<br />
              Platform 5%
            </div>
          </WFCard>
        </div>
        <div style={{ marginTop: 18 }}>
          <WFTitle level={2}>Reservation lifecycle</WFTitle>
          <div style={{ marginTop: 10, display: 'flex', alignItems: 'center', gap: 6, flexWrap: 'wrap', fontFamily: wfFontMono, fontSize: 10 }}>
            {['Submitted','PendingHostMarinaApproval','PendingApproval','Confirmed','Completed'].map((s, i) => (
              <React.Fragment key={s}>
                <span style={{ padding: '4px 8px', border: `1.5px solid ${WF_INK}`, background: i===3 ? WF_HIGHLIGHT : '#fff', filter: 'url(#wf-rough)' }}>{s}</span>
                {i < 4 && <span>→</span>}
              </React.Fragment>
            ))}
          </div>
          <div style={{ marginTop: 8, display: 'flex', gap: 10, flexWrap: 'wrap', fontFamily: wfFontMono, fontSize: 10 }}>
            <span style={{ padding: '4px 8px', border: `1.5px dashed ${WF_BAD}`, color: WF_BAD }}>→ Declined</span>
            <span style={{ padding: '4px 8px', border: `1.5px dashed ${WF_BAD}`, color: WF_BAD }}>→ Cancelled</span>
            <span style={{ padding: '4px 8px', border: `1.5px dashed ${WF_INK_SOFT}`, color: WF_INK_SOFT }}>→ NoShow</span>
          </div>
          <WFAnnotation rotate={-1} style={{ position: 'static', marginTop: 10 }}>
            Branching depends on Slip.HostMarinaPolicy &amp; AvailabilityWindow.InstantBook
          </WFAnnotation>
        </div>
      </div>
    </WFPage>
  );
}

Object.assign(window, {
  FramePrivateOnboard, FramePrivateDashboard, FramePrivateDockominium,
  FramePlatformTenants, FramePlatformModeration, FramePlatformUser,
  FrameSignIn, FrameVesselClaim, FrameSubletDiagram,
});

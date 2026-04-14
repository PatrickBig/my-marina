using Microsoft.AspNetCore.Mvc;

namespace MyMarina.Api.Controllers;

/// <summary>
/// Liveness and readiness probes for Kubernetes.
/// /health — liveness (is the process alive?)
/// /ready  — readiness (are dependencies reachable?)
/// Both are also mapped via MapHealthChecks in Program.cs.
/// This controller exists for documentation visibility only.
/// </summary>
[ApiController]
[ApiExplorerSettings(IgnoreApi = true)]
public class HealthController : ControllerBase;

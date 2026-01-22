[ApiController]
[Route("Plugins/TwoFA")]
public sealed class TwoFactorController : ControllerBase
{
    private readonly ITwoFactorUserStore _store;
    private readonly ITotpService _totp;
    private readonly IUserManager _userManager;
    private readonly ISessionManager _sessionManager;

    public TwoFactorController(ITwoFactorUserStore store, ITotpService totp, IUserManager userManager, ISessionManager sessionManager)
    {
        _store = store;
        _totp = totp;
        _userManager = userManager;
        _sessionManager = sessionManager;
    }

    [HttpPost("users/{userId:guid}/totp/enroll")]
    public async Task<ActionResult<EnrollResponse>> Enroll(Guid userId, CancellationToken ct)
    {
        var secret = _totp.GenerateSecret();
        var settings = await _store.GetAsync(userId, ct);
        settings.TotpSecret = secret;
        settings.IsTotpConfirmed = false;
        await _store.SetAsync(userId, settings, ct);

        var user = await _userManager.GetUserByIdAsync(userId).ConfigureAwait(false);
        var uri = _totp.BuildOtpAuthUri("Jellyfin", user?.Username ?? "Jellyfin", secret);

        return new EnrollResponse { Secret = secret, OtpAuthUri = uri };
    }

    [HttpPost("users/{userId:guid}/totp/confirm")]
    public async Task<ActionResult> Confirm(Guid userId, [FromBody] ConfirmRequest request, CancellationToken ct)
    {
        var settings = await _store.GetAsync(userId, ct);
        if (settings.TotpSecret is null || !_totp.ValidateCode(settings.TotpSecret, request.Code))
        {
            return BadRequest("Invalid code.");
        }

        settings.IsEnabled = true;
        settings.IsTotpConfirmed = true;
        await _store.SetAsync(userId, settings, ct);
        return Ok();
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthenticationResult>> Login([FromBody] LoginRequest request)
    {
        // 1) authenticate username/password (create session)
        // 2) if user has 2FA enabled -> validate OTP
        // 3) return the authentication result/token
    }
}

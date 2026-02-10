using System.Threading.Tasks;
using Robust.Shared.Configuration;
using Robust.Shared.Network;

namespace Content.Server._Reserve.LenaApi;

public sealed class LenaApiManager
{
    [Dependency] private readonly IConfigurationManager _configurationManager = default!;
    //[Dependency] private readonly IPlayerManager _playerManager = default!;
    private ISawmill _sawmill = default!;

    private ApiWrapper? _wrapper;
    private string ApiToken => _configurationManager.GetCVar(LenaApiCVars.ApiKey);
    public bool IsIntegrationEnabled => _configurationManager.GetCVar(LenaApiCVars.ApiIntegration);
    public string BaseUri => _configurationManager.GetCVar(LenaApiCVars.BaseUri);
    public ApiWrapper? Wrapper => _wrapper;
    private readonly Dictionary<string, User> _users = new();

    public void Initialize()
    {
        _wrapper = new ApiWrapper(BaseUri, () => ApiToken);
        _configurationManager.OnValueChanged(LenaApiCVars.BaseUri, newUri => _wrapper.SetBaseUri(newUri), true);
    }

    public async Task UpdateUserData(NetUserId netUserId)
    {
        var userRequest = await GetUserFromApi(netUserId.ToString());

        if (!userRequest.IsSuccess || userRequest.Value == null)
            return;

        await UpdateUserData(netUserId, userRequest.Value);
    }

    public async Task UpdateUserData(NetUserId netUserId, ApiWrapper.UserRead userReadData)
    {
        var inventoryRequest = await GetInventoryFromApi(userReadData.Id);

        if (!inventoryRequest.IsSuccess || inventoryRequest.Value == null)
        {
            _sawmill.Error($"Could not read inventory from API for user {userReadData.Id}, Error = {inventoryRequest.Error}");
            return;
        }

        _users[netUserId.ToString()] = new User(userReadData, inventoryRequest.Value);
    }

    public User? GetUser(NetUserId netUserId)
    {
        _users.TryGetValue(netUserId.ToString(), out var user);
        return user;
    }

    public async Task<string?> ShouldDenyConnection(NetUserId netUserId)
    {
        if (IsIntegrationEnabled)
        {
            var response = await GetUserFromApi(netUserId.ToString());
            if (!response.IsSuccess)
            {
                if (response.Error is ApiWrapper.NotFoundError)
                    return Loc.GetString("reserve-auth-required");

                _sawmill.Error($"Got unhandled response while denying user {netUserId}:\n{response.Error}");
                return Loc.GetString("reserve-auth-error");
            }
            if (response.Value != null)
                await UpdateUserData(new NetUserId(netUserId), response.Value);
        }
        return null;
    }

    #region api methods

    public async Task<ApiWrapper.Result<ApiWrapper.UserRead>> GetUserFromApi(string ss14Id)
    {
        if (!IsIntegrationEnabled)
            return ApiWrapper.Result<ApiWrapper.UserRead>.Failure(new IntegrationDisabledError());

        if (_wrapper == null)
            throw new Exception("ApiWrapper not initialized");

        return await _wrapper.GetUser(ss14Id);
    }

    public async Task<ApiWrapper.Result<ApiWrapper.Inventory>> GetInventoryFromApi(int id)
    {
        if (!IsIntegrationEnabled)
            return ApiWrapper.Result<ApiWrapper.Inventory>.Failure(new IntegrationDisabledError());

        if (_wrapper == null)
            throw new Exception("ApiWrapper is not initialized");

        return await _wrapper.GetInventory(id);
    }

    public async Task<ApiWrapper.Result<ApiWrapper.BalanceModify>> ModifyBalanceInApi(int id, int? reserveCoins = null, int? donateCoins = null)
    {
        if (!IsIntegrationEnabled)
            return ApiWrapper.Result<ApiWrapper.BalanceModify>.Failure(new IntegrationDisabledError());

        if (_wrapper == null)
            return ApiWrapper.Result<ApiWrapper.BalanceModify>.Failure(new WrapperNotInitializedError());

        return await _wrapper.PostEditBalance(id,
            new ApiWrapper.BalanceModify
            {
            ReserveCoins = reserveCoins,
            DonateCoins = donateCoins
        });
    }

    public async Task<ApiWrapper.Result<ApiWrapper.UserRead>> ModifyUser(int id, ApiWrapper.UserAdminModify userAdminModify)
    {
        if (!IsIntegrationEnabled)
            return ApiWrapper.Result<ApiWrapper.UserRead>.Failure(new IntegrationDisabledError());

        if (_wrapper == null)
            return ApiWrapper.Result<ApiWrapper.UserRead>.Failure(new WrapperNotInitializedError());

        return await _wrapper.PatchUser(id, userAdminModify);
    }

    #endregion

    public record IntegrationDisabledError : ApiWrapper.ApiError;
    public record WrapperNotInitializedError : ApiWrapper.ApiError;
}

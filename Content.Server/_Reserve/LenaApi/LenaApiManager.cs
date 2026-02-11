// SPDX-FileCopyrightText: 2026 Space Station 14 Contributors
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Threading.Tasks;
using Robust.Shared.Configuration;
using Robust.Shared.Network;

namespace Content.Server._Reserve.LenaApi;

public sealed class LenaApiManager
{
    [Dependency] private readonly IConfigurationManager _configurationManager = default!;
    private ISawmill _sawmill = default!;

    private ApiWrapper? _wrapper;
    public ApiWrapper? Wrapper => _wrapper;
    [ViewVariables] private readonly Dictionary<string, User> _users = new();
    [ViewVariables] private Dictionary<int, ApiWrapper.ItemRarityList.Entry> _rarityNames = new();
    [ViewVariables] private Dictionary<int, ApiWrapper.SubTierList.Entry> _subLevelNames = new();

    private string ApiToken => _configurationManager.GetCVar(LenaApiCVars.ApiKey);
    public bool IsIntegrationEnabled => _configurationManager.GetCVar(LenaApiCVars.ApiIntegration);
    public bool IsAuthRequired => _configurationManager.GetCVar(LenaApiCVars.RequireAuth);
    public string BaseUri => _configurationManager.GetCVar(LenaApiCVars.BaseUri);

    public void Initialize()
    {
        _wrapper = new ApiWrapper(BaseUri, () => ApiToken);
        _configurationManager.OnValueChanged(LenaApiCVars.BaseUri, newUri => _wrapper.SetBaseUri(newUri), true);
        _configurationManager.OnValueChanged(LenaApiCVars.ApiIntegration, newUri => _ = UpdateData(), true);

        _ = UpdateData();
    }

    public async Task UpdateData()
    {
        if (_wrapper == null)
            return;

        var subLevelNames = await _wrapper.GetDonorsTiers();
        if (subLevelNames is { IsSuccess: true, Value: not null })
        {
            _subLevelNames = subLevelNames.Value.AsDictionary();
        }

        var rarityNames = await _wrapper.GetInventoryRarities();
        if (rarityNames is { IsSuccess: true, Value: not null })
        {
            _rarityNames = rarityNames.Value.AsDictionary();
        }
    }

    public string? GetSubLevelName(int subLevel)
    {
        _subLevelNames.TryGetValue(subLevel, out var entry);
        return entry?.Label;
    }

    public string? GetRarityName(int rarity)
    {
        _rarityNames.TryGetValue(rarity, out var entry);
        return entry?.Label;
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
        if (IsIntegrationEnabled && IsAuthRequired)
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

    private async Task<ApiWrapper.Result<T>> Send<T>(Func<ApiWrapper, Task<ApiWrapper.Result<T>>> func)
    {
        if (!IsIntegrationEnabled)
            return ApiWrapper.Result<T>.Failure(new IntegrationDisabledError());

        if (_wrapper == null)
            return ApiWrapper.Result<T>.Failure(new WrapperNotInitializedError());

        return await func(_wrapper);
    }

    #region api methods

    public async Task<ApiWrapper.Result<ApiWrapper.UserRead>> GetUserFromApi(string ss14Id) =>
        await Send<ApiWrapper.UserRead>(wrapper => wrapper.GetUser(ss14Id));

    public async Task<ApiWrapper.Result<ApiWrapper.InventoryRead>> GetInventoryFromApi(int id) =>
        await Send<ApiWrapper.InventoryRead>(wrapper => wrapper.GetInventory(id));

    #endregion

    public record IntegrationDisabledError : ApiWrapper.ApiError;
    public record WrapperNotInitializedError : ApiWrapper.ApiError;
}

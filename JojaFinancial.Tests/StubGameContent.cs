﻿using StardewModdingAPI;
using StardewValley;

namespace JojaFinancial.Tests
{
    public class StubGameContent : IGameContentHelper
    {
        string IGameContentHelper.CurrentLocale => throw new NotImplementedException();

        LocalizedContentManager.LanguageCode IGameContentHelper.CurrentLocaleConstant => throw new NotImplementedException();

        string IModLinked.ModID => throw new NotImplementedException();

        IAssetData IGameContentHelper.GetPatchHelper<T>(T data, string? assetName)
        {
            throw new NotImplementedException();
        }

        bool IGameContentHelper.InvalidateCache(string assetName)
        {
            return false;
        }

        bool IGameContentHelper.InvalidateCache(IAssetName assetName)
        {
            return false;
        }

        bool IGameContentHelper.InvalidateCache<T>()
        {
            return false;
        }

        bool IGameContentHelper.InvalidateCache(Func<IAssetInfo, bool> predicate)
        {
            return false;
        }

        T IGameContentHelper.Load<T>(string assetName)
        {
            throw new NotImplementedException();
        }

        T IGameContentHelper.Load<T>(IAssetName assetName)
        {
            throw new NotImplementedException();
        }

        IAssetName IGameContentHelper.ParseAssetName(string rawName)
        {
            throw new NotImplementedException();
        }
    }

}

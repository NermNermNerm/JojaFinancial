using System;
using StardewModdingAPI;
using StardewModdingAPI.Events;

namespace JojaFinancial.Tests
{
    // Creates an implementation of IModHelper that the game can use and stub implementations
    // for some features in the game.
    public class StubModHelper
        : IModHelper
    {

        private class StubContentEvents : IContentEvents
        {
            public event EventHandler<AssetRequestedEventArgs>? AssetRequested { add { } remove { } }
            public event EventHandler<AssetsInvalidatedEventArgs>? AssetsInvalidated { add { } remove { } }
            public event EventHandler<AssetReadyEventArgs>? AssetReady { add { } remove { } }
            public event EventHandler<LocaleChangedEventArgs>? LocaleChanged { add { } remove { } }
        }

        public class StubGameLoopEvents : IGameLoopEvents
        {
            public void RaiseEndOfDay()
            {
                this.DayEnding?.Invoke(this, new DayEndingEventArgs());
            }

            public event EventHandler<GameLaunchedEventArgs>? GameLaunched { add { } remove { } }
            public event EventHandler<UpdateTickingEventArgs>? UpdateTicking { add { } remove { } }
            public event EventHandler<UpdateTickedEventArgs>? UpdateTicked { add { } remove { } }
            public event EventHandler<OneSecondUpdateTickingEventArgs>? OneSecondUpdateTicking { add { } remove { } }
            public event EventHandler<OneSecondUpdateTickedEventArgs>? OneSecondUpdateTicked { add { } remove { } }
            public event EventHandler<SaveCreatingEventArgs>? SaveCreating { add { } remove { } }
            public event EventHandler<SaveCreatedEventArgs>? SaveCreated { add { } remove { } }
            public event EventHandler<SavingEventArgs>? Saving { add { } remove { } }
            public event EventHandler<SavedEventArgs>? Saved { add { } remove { } }
            public event EventHandler<SaveLoadedEventArgs>? SaveLoaded { add { } remove { } }
            public event EventHandler<DayStartedEventArgs>? DayStarted { add { } remove { } }
            public event EventHandler<DayEndingEventArgs>? DayEnding;
            public event EventHandler<TimeChangedEventArgs>? TimeChanged { add { } remove { } }
            public event EventHandler<ReturnedToTitleEventArgs>? ReturnedToTitle { add { } remove { } }
        }

        private class StubModEvents : IModEvents
        {
            public IContentEvents Content { get; } = new StubContentEvents();

            IDisplayEvents IModEvents.Display => throw new NotImplementedException();

            IGameLoopEvents IModEvents.GameLoop { get; } = new StubGameLoopEvents();

            IInputEvents IModEvents.Input => throw new NotImplementedException();

            IMultiplayerEvents IModEvents.Multiplayer => throw new NotImplementedException();

            IPlayerEvents IModEvents.Player => throw new NotImplementedException();

            IWorldEvents IModEvents.World => throw new NotImplementedException();

            ISpecializedEvents IModEvents.Specialized => throw new NotImplementedException();
        }

        public StubGameContent StubGameContent { get; } = new StubGameContent();

        string IModHelper.DirectoryPath => throw new NotImplementedException();

        public IModEvents Events { get; } = new StubModEvents();

        ICommandHelper IModHelper.ConsoleCommands => throw new NotImplementedException();

        IGameContentHelper IModHelper.GameContent => this.StubGameContent;

        IModContentHelper IModHelper.ModContent => throw new NotImplementedException();

        IContentPackHelper IModHelper.ContentPacks => throw new NotImplementedException();

        IDataHelper IModHelper.Data => throw new NotImplementedException();

        IInputHelper IModHelper.Input => throw new NotImplementedException();

        IReflectionHelper IModHelper.Reflection => throw new NotImplementedException();

        IModRegistry IModHelper.ModRegistry => throw new NotImplementedException();

        IMultiplayerHelper IModHelper.Multiplayer => throw new NotImplementedException();

        ITranslationHelper IModHelper.Translation => throw new NotImplementedException();

        TConfig IModHelper.ReadConfig<TConfig>()
        {
            throw new NotImplementedException();
        }

        void IModHelper.WriteConfig<TConfig>(TConfig config)
        {
            throw new NotImplementedException();
        }
    }

}

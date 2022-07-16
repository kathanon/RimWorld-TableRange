using HugsLib.Utils;
using RimWorld;
using Verse;


namespace TableRange
{
    public class Main : HugsLib.ModBase
    {
        public Main()
        {
            Instance = this;
        }

        internal new ModLogger Logger => base.Logger;

        internal static Main Instance { get; private set; }

        public override string ModIdentifier => Strings.MOD_IDENTIFIER;

        public override void DefsLoaded()
        {
            MySettings.Setup(Settings);
            State.Setup();
        }
    }
}

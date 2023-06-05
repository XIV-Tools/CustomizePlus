// © Customize+.
// Licensed under the MIT license.

using System;

using CustomizePlus.Core;
using CustomizePlus.Helpers;

using Dalamud.Hooking;

using FFXIVClientStructs.FFXIV.Client.System.Framework;

namespace CustomizePlus.Services
{
    //Borrowed from Brio
    internal class GPoseService : ServiceBase<GPoseService>
    {
        public delegate void OnGPoseStateDelegate(GPoseState gposeState);

        private Hook<EnterGPoseDelegate>? _enterGPoseHook;
        private Hook<ExitGPoseDelegate>? _exitGPoseHook;

        private bool _fakeGPose;
        public GPoseState GPoseState { get; private set; }
        public bool IsInGPose => GPoseState == GPoseState.Inside;

        public bool FakeGPose
        {
            get => _fakeGPose;

            set
            {
                if (value != _fakeGPose)
                {
                    if (!value)
                    {
                        _fakeGPose = false;
                        HandleGPoseChange(GPoseState.Exiting);
                        HandleGPoseChange(GPoseState.Outside);
                    }
                    else
                    {
                        HandleGPoseChange(GPoseState.Inside);
                        _fakeGPose = true;
                    }
                }
            }
        }

        public event OnGPoseStateDelegate? OnGPoseStateChange;

        public override unsafe void Start()
        {
            GPoseState = DalamudServices.PluginInterface.UiBuilder.GposeActive ? GPoseState.Inside : GPoseState.Outside;

            var uiModule = Framework.Instance()->GetUiModule();
            var enterGPoseAddress = (nint)uiModule->VTable->EnterGPose;
            var exitGPoseAddress = (nint)uiModule->VTable->ExitGPose;

            _enterGPoseHook = Hook<EnterGPoseDelegate>.FromAddress(enterGPoseAddress, EnteringGPoseDetour);
            _enterGPoseHook.Enable();

            _exitGPoseHook = Hook<ExitGPoseDelegate>.FromAddress(exitGPoseAddress, ExitingGPoseDetour);
            _exitGPoseHook.Enable();

            base.Start();
        }

        private void ExitingGPoseDetour(IntPtr addr)
        {
            if (HandleGPoseChange(GPoseState.AttemptExit))
            {
                HandleGPoseChange(GPoseState.Exiting);
                _exitGPoseHook!.Original.Invoke(addr);
                HandleGPoseChange(GPoseState.Outside);
            }
        }

        private bool EnteringGPoseDetour(IntPtr addr)
        {
            var didEnter = _enterGPoseHook!.Original.Invoke(addr);
            if (didEnter)
            {
                _fakeGPose = false;
                HandleGPoseChange(GPoseState.Inside);
            }

            return didEnter;
        }

        private bool HandleGPoseChange(GPoseState state)
        {
            if (state == GPoseState || _fakeGPose)
            {
                return true;
            }

            GPoseState = state;

            try
            {
                OnGPoseStateChange?.Invoke(state);
            }
            catch (Exception e)
            {
                ChatHelper.PrintInChat($"GPose transition error.\n Reason: {e.Message}");
                return false;
            }

            return true;
        }

        public override void Dispose()
        {
            _exitGPoseHook?.Dispose();
            _enterGPoseHook?.Dispose();
        }

        private delegate void ExitGPoseDelegate(IntPtr addr);

        private delegate bool EnterGPoseDelegate(IntPtr addr);
    }

    public enum GPoseState
    {
        Inside,
        AttemptExit,
        Exiting,
        Outside
    }
}
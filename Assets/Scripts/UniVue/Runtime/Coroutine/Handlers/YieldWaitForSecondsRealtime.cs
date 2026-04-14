using System;
using UnityEngine;

namespace Framwork.Coroutine
{
    public sealed class YieldWaitForSecondsRealtime : YieldHandler
    {
        public override Type YieldType => typeof(WaitForSecondsRealtime);

        protected override bool HandleYield(CoroutineMgr.CoroutineRecorder recorder)
        {
            WaitForSecondsRealtime waitForSecondsRealtime = (WaitForSecondsRealtime)recorder.Yield;
            return !waitForSecondsRealtime.keepWaiting;
        }
    }
}
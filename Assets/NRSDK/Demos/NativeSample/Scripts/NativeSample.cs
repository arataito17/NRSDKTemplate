using NRKernal;
using System;
using System.Collections;
using System.Runtime.InteropServices;
using UnityEngine;

public class NativeSample : MonoBehaviour
{
    private void Awake()
    {
        StartCoroutine(Co_DelayInitConsumer());
    }

    private IEnumerator Co_DelayInitConsumer()
    {
        yield return new WaitForSeconds(2);
        Debug.Log("[NativeSample] Initialize NativeConsumer");
#if ENABLE_NATIVE_SESSION_MANAGER
        NativeConsumer.Initialize();
#endif
    }

}

public static class NativeConsumer
{
    const string CONSUMER_LIB = "libConsumerPlugin";

    [DllImport(CONSUMER_LIB)]
    internal extern static void Initialize();
}
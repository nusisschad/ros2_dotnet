// Copyright 2016-2018 Esteve Fernandez <esteve@apache.org>
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

using ROS2.Common;
using ROS2.Interfaces;
using ROS2.Utils;

namespace ROS2 {
  internal class NodeDelegates {
    private static readonly DllLoadUtils dllLoadUtils;

    [UnmanagedFunctionPointer (CallingConvention.Cdecl)]
    internal delegate int NativeRCLCreatePublisherHandleType (
      ref IntPtr publisherHandle, IntPtr node_handle, [MarshalAs (UnmanagedType.LPStr)] string node_name, IntPtr typesupport_ptr);

    internal static NativeRCLCreatePublisherHandleType native_rcl_create_publisher_handle = null;

    [UnmanagedFunctionPointer (CallingConvention.Cdecl)]
    internal delegate int NativeRCLCreateSubscriptionHandleType (
      ref IntPtr subscriptionHandle, IntPtr node_handle, [MarshalAs (UnmanagedType.LPStr)] string node_name, IntPtr typesupport_ptr);

    internal static NativeRCLCreateSubscriptionHandleType native_rcl_create_subscription_handle = null;

    static NodeDelegates () {
      dllLoadUtils = DllLoadUtilsFactory.GetDllLoadUtils ();
      try {
        IntPtr nativelibrary = dllLoadUtils.LoadLibrary ("rcldotnet_node");

        IntPtr native_rcl_create_publisher_handle_ptr = dllLoadUtils.GetProcAddress (
          nativelibrary, "native_rcl_create_publisher_handle");

        NodeDelegates.native_rcl_create_publisher_handle =
          (NativeRCLCreatePublisherHandleType) Marshal.GetDelegateForFunctionPointer (
            native_rcl_create_publisher_handle_ptr, typeof (NativeRCLCreatePublisherHandleType));

        IntPtr native_rcl_create_subscription_handle_ptr = dllLoadUtils.GetProcAddress (
          nativelibrary, "native_rcl_create_subscription_handle");

        NodeDelegates.native_rcl_create_subscription_handle =
          (NativeRCLCreateSubscriptionHandleType) Marshal.GetDelegateForFunctionPointer (
            native_rcl_create_subscription_handle_ptr, typeof (NativeRCLCreateSubscriptionHandleType));
      } catch (UnsatisfiedLinkError e) {
        System.Console.WriteLine ("Native code library failed to load.\n" + e);
        Environment.Exit (1);
      }
    }
  }

  public class Node : INode {

    private IList<ISubscription> subscriptions_;

    private IntPtr nodeHandle_;

    public Node (IntPtr node_handle) {
      nodeHandle_ = node_handle;
      subscriptions_ = new List<ISubscription> ();
    }

    public IList<ISubscription> Subscriptions { get { return subscriptions_; } }

    public IntPtr Handle { get { return nodeHandle_; } }

    public Publisher<T> CreatePublisher<T> (string topic) where T : IMessage {
      Type typeParametertype = typeof (T);
      MethodInfo m = typeParametertype.GetMethod ("_GET_TYPE_SUPPORT");

      IntPtr typesupport = (IntPtr) m.Invoke (null, new object[] { });
      IntPtr publisherHandle = IntPtr.Zero;
      RCLRet ret = (RCLRet) NodeDelegates.native_rcl_create_publisher_handle (ref publisherHandle, nodeHandle_, topic, typesupport);
      Publisher<T> publisher = new Publisher<T> (publisherHandle);
      return publisher;
    }

    public Subscription<T> CreateSubscription<T> (string topic, Action<T> callback) where T : IMessage, new () {
      Type typeParametertype = typeof (T);
      MethodInfo m = typeParametertype.GetMethod ("_GET_TYPE_SUPPORT");

      IntPtr typesupport = (IntPtr) m.Invoke (null, new object[] { });
      IntPtr subscriptionHandle = IntPtr.Zero;
      RCLRet ret = (RCLRet) NodeDelegates.native_rcl_create_subscription_handle (ref subscriptionHandle, nodeHandle_, topic, typesupport);
      Subscription<T> subscription = new Subscription<T> (subscriptionHandle, callback);
      this.subscriptions_.Add (subscription);
      return subscription;
    }
  }
}
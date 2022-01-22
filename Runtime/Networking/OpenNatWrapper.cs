using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Open.Nat;
using UnityEngine;

namespace Tehelee.Baseline
{
	public static class OpenNatWrapper
	{
		[RuntimeInitializeOnLoadMethod( RuntimeInitializeLoadType.AfterAssembliesLoaded )]
		private static void OnAssembliesLoaded()
		{
			natDiscoverer = new NatDiscoverer();
			discoveredNatDevice = null;
		}
		
		////////////////////////////////
		#region Attributes

		public static NatDiscoverer natDiscoverer = new NatDiscoverer();

		private static NatDevice discoveredNatDevice = null;
		
		#endregion

		////////////////////////////////
		#region Discovery

		public static void DiscoverDevice( System.Action<NatDevice> callback )
		{
			void OnDiscoverDevice( NatDevice natDevice )
			{
				discoveredNatDevice = natDevice;
				callback?.Invoke( natDevice );
			}
			
			if( object.Equals( null, discoveredNatDevice ) && !Utils.IsShuttingDown )
				Utils.WaitForTask
				(
					natDiscoverer.DiscoverDeviceAsync
					(
						PortMapper.Upnp,
						new System.Threading.CancellationTokenSource( 3000 )
					),
					OnDiscoverDevice
				);
			else
				callback?.Invoke( discoveredNatDevice );
		}

		public static void GetExternalIP( this NatDevice natDevice, System.Action<IPAddress> callback )
		{
			if( object.Equals( null, natDevice ) )
			{
				callback?.Invoke( IPAddress.Any );
				return;
			}

			Utils.WaitForTask( natDevice.GetExternalIPAsync(), callback );
		}

		public static void GetInternalIP( bool ipv4, System.Action<IPAddress> callback )
		{
			try
			{
				using( Socket socket = new Socket( ipv4 ? AddressFamily.InterNetwork : AddressFamily.InterNetworkV6, SocketType.Dgram, 0 ) )
				{
					socket.Connect( "8.8.8.8", 65530 );
					IPEndPoint endPoint = socket.LocalEndPoint as IPEndPoint;

					if( object.Equals( null, endPoint ) )
						callback?.Invoke( ipv4 ? IPAddress.Any : IPAddress.IPv6Any );
					else
						callback?.Invoke( endPoint.Address );
				}
			}
			catch
			{
				callback?.Invoke( IPAddress.Any );
			}
		}

		#endregion

		////////////////////////////////
		#region GetPortMappings

		public static void GetPortMappings( int discoverTimeoutMS, System.Action<List<Mapping>> callback )
		{
			DiscoverDevice( ( natDevice ) => GetPortMappings( natDevice, discoverTimeoutMS, callback ) );
		}
		public static void GetPortMappings( this NatDevice natDevice, int discoverTimeoutMS, System.Action<List<Mapping>> callback )
		{
			if( object.Equals( null, natDevice ) )
			{
				callback?.Invoke( new List<Mapping>() );
				return;
			}

			Task.Run
			(
				async () =>
				{
					List<Mapping> mappings = new List<Mapping>();
					try
					{
						IEnumerable<Mapping> mappingsRaw = await natDevice.GetAllMappingsAsync();
						mappings.AddRange( mappingsRaw );
					}
					catch( Exception e )
					{
						Debug.LogWarning( $"Open.NAT - Unable to load port mappings...\n{e.Message}" );
					}
					
					callback?.Invoke( mappings );
				},
				new System.Threading.CancellationTokenSource( 30000 ).Token
			);
		}

		#endregion

		////////////////////////////////
		#region CreatePortMapping
		
		public enum PortMappingResult
		{
			Success = 0,
			InvalidArgs,
			PortInUse,
			MappingTableFull,
			DeviceNotFound,
			Failure,
			InvalidPermissions,
			NotSupported
		}

		public static void CreatePortMapping( Mapping mapping, System.Action<PortMappingResult> callback = null )
		{
			if( object.Equals( null, mapping ) )
			{
				callback?.Invoke( PortMappingResult.InvalidArgs );
				return;
			}

			DiscoverDevice( ( NatDevice natDevice ) => CreatePortMapping( natDevice, mapping, callback ) );
		}

		public static void CreatePortMapping( this NatDevice natDevice, Mapping mapping, System.Action<PortMappingResult> callback = null )
		{
			if( object.Equals( null, natDevice ) || object.Equals( null, mapping ) )
			{
				callback?.Invoke( PortMappingResult.InvalidArgs );
				return;
			}
			
			try
			{
				Debug.Log
				(
					$"Open.NAT - Mapping Port {mapping.PrivateIP}:{mapping.PublicPort}\n" +
					$"Device: {natDevice}"
				);

				void OnCreatePortMapping()
				{
					Debug.Log
					(
						$"Open.NAT - Mapped Port {mapping.PrivateIP}:{mapping.PublicPort}\n" +
						$"Device: {natDevice}"
					);
					callback?.Invoke( PortMappingResult.Success );
				}
				
				Utils.WaitForTask( natDevice.CreatePortMapAsync( mapping ), OnCreatePortMapping );
			}
			catch( NatDeviceNotFoundException )
			{
				callback?.Invoke( PortMappingResult.DeviceNotFound );
			}
			catch( MappingException mappingException )
			{
				switch( mappingException.ErrorCode )
				{
					default:
					case 402: // InvalidArguments
					case 713: // SpecifiedArrayIndexInvalid
					case 714: // NoSuchEntryInArray
					case 715: // WildCardNotPermittedInSourceIp
					case 716: // WildCardNotPermittedInExternalPort
					case 724: // SamePortValuesRequired
					case 726: // RemoteHostOnlySupportsWildcard
					case 727: // ExternalPortOnlySupportsWildcard
					case 732: // WildCardNotPermittedInIntPort
						callback?.Invoke( PortMappingResult.InvalidArgs );
						break;

					case 501: // ActionFailed
						callback?.Invoke( PortMappingResult.Failure );
						break;

					case 606: // Unathorized
						callback?.Invoke( PortMappingResult.InvalidPermissions );
						break;
					
					case 718: // ConflictInMappingEntry
					case 729: // ConflictWithOtherMechanisms
						callback?.Invoke( PortMappingResult.PortInUse );
						break;

					case 725: // OnlyPermanentLeasesSupported
						callback?.Invoke( PortMappingResult.NotSupported );
						break;

					case 728: // NoPortMapsAvailable
						callback?.Invoke( PortMappingResult.MappingTableFull );
						break;
				}
			}
		}

		#endregion

		////////////////////////////////
		#region DeletePortMapping

		public static void DeletePortMapping( Mapping mapping, System.Action callback = null )
		{
			if( object.Equals( null, mapping ) )
			{
				callback?.Invoke();
				return;
			}

			DiscoverDevice( ( NatDevice natDevice ) => DeletePortMapping( natDevice, mapping, callback ) );
		}

		public static void DeletePortMapping( this NatDevice natDevice, Mapping mapping, System.Action callback = null )
		{
			if( object.Equals( null, natDevice ) || object.Equals( null, mapping ) )
			{
				callback?.Invoke();
				return;
			}

			void OnDelete()
			{
				Debug.Log
				(
					$"Open.NAT - Unmapped {mapping.PrivateIP}:{mapping.PublicPort}\n" +
					$"Device: {natDevice}"
				);
				callback?.Invoke();
			}

			if( Utils.IsShuttingDown )
			{
				Debug.LogWarning
				(
					$"Open.NAT - Unmapping {mapping.PrivateIP}:{mapping.PublicPort}\n" +
					"!!! Application Shutting Down, unmapping without awaiter...\n" +
					$"Device: {natDevice}"
				);
				natDevice.DeletePortMapAsync( mapping );
				callback?.Invoke();
			}
			else
			{
				Debug.Log( $"Open.NAT - Unmapping {mapping.PrivateIP}:{mapping.PublicPort}\nDevice: {natDevice}" );
				Utils.WaitForTask( natDevice.DeletePortMapAsync( mapping ), OnDelete );
			}
		}

		#endregion
	}
}
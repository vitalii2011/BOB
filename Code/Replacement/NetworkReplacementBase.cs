﻿using System.Collections.Generic;
using UnityEngine;


namespace BOB
{
	/// <summary>
	/// Records original prop data.
	/// </summary>
	public class NetPropReference
	{
		public NetInfo netInfo;
		public int laneIndex;
		public int propIndex;
		public float angle;
		public Vector3 position;
		public int probability;
	}


	/// <summary>
	/// Base class for network replacement.
	/// </summary>
	internal abstract class NetworkReplacementBase
	{
		/// <summary>
		/// Returns the config file list of network elements relevant to the current replacement type.
		/// </summary>
		protected abstract List<BOBNetworkElement> NetworkElementList { get; }


		/// <summary>
		/// Retrieves a currently-applied replacement entry for the given network, lane and prop index.
		/// </summary>
		/// <param name="networkInfo">Network prefab</param>
		/// <param name="targetInfo">Target prop/tree prefab</param>
		/// <param name="laneIndex">Lane number</param>
		/// <param name="propIndex">Prop index number</param>
		/// <returns>Currently-applied feplacement (null if none)</returns>
		internal abstract BOBNetReplacement EligibileReplacement(NetInfo netInfo, PrefabInfo targetInfo, int laneIndex, int propIndex);


		/// <summary>
		/// Reverts all active replacements.
		/// </summary>
		internal virtual void RevertAll()
		{
			// Iterate through each entry in the replacement list.
			foreach (BOBNetworkElement netElement in NetworkElementList)
			{
				foreach (BOBNetReplacement replacement in netElement.replacements)
				{
					// Revert this replacement (but don't remove the entry, as the list is currently immutable while we're iterating through it).
					Revert(replacement, false);
				}
			}
		}


		/// <summary>
		/// Applies a new (or updated) replacement.
		/// </summary>
		/// <param name="netInfo">Targeted network prefab</param>
		/// <param name="targetInfo">Targeted (original) prop prefab</param>
		/// <param name="replacementInfo">Replacment prop prefab</param>
		/// <param name="laneIndex">Targeted lane index (in parent network)</param>
		/// <param name="propIndex">Prop index to apply replacement to</param>
		/// <param name="angle">Replacment prop angle adjustment</param>
		/// <param name="offsetX">Replacment X position offset</param>
		/// <param name="offsetY">Replacment Y position offset</param>
		/// <param name="offsetZ">Replacment Z position offset</param>
		/// <param name="probability">Replacement probability</param>
		internal void Replace(NetInfo netInfo, PrefabInfo targetInfo, PrefabInfo replacementInfo, int laneIndex, int propIndex, float angle, float offsetX, float offsetY, float offsetZ, int probability)
		{
			// Make sure that target and replacement are the same type before doing anything.
			if (targetInfo?.name == null || replacementInfo?.name == null || (targetInfo is TreeInfo && !(replacementInfo is TreeInfo)) || (targetInfo is PropInfo) && !(replacementInfo is PropInfo))
			{
				return;
			}

			// Revert any current replacement entry for this prop.
			Revert(netInfo, targetInfo, laneIndex, propIndex, true);

			// Get configuration file network list entry.
			List<BOBNetReplacement> replacementsList = ReplacementEntry(netInfo);

			// Get current replacement after reversion above.
			BOBNetReplacement thisReplacement = EligibileReplacement(netInfo, targetInfo, laneIndex, propIndex);

			// Create new replacement list entry if none already exists.
			if (thisReplacement == null)
			{
				thisReplacement = new BOBNetReplacement
				{
					netInfo = netInfo,
					target = targetInfo.name,
					targetInfo = targetInfo,
					laneIndex = laneIndex,
					propIndex = propIndex
				};
				replacementsList.Add(thisReplacement);
			}

			// Add/replace replacement data.
			thisReplacement.tree = targetInfo is TreeInfo;
			thisReplacement.angle = angle;
			thisReplacement.offsetX = offsetX;
			thisReplacement.offsetY = offsetY;
			thisReplacement.offsetZ = offsetZ;
			thisReplacement.probability = probability;

			// Record replacement prop.
			thisReplacement.replacementInfo = replacementInfo;
			thisReplacement.Replacement = replacementInfo.name;

			// Apply replacement.
			ApplyReplacement(thisReplacement);
		}


		/// <summary>
		/// Reverts a replacement.
		/// </summary>
		/// <param name="netInfo">Targeted network prefab</param>
		/// <param name="targetInfo">Targeted (original) tree/prop prefab</param>
		/// <param name="laneIndex">Targeted (original) tree/prop lane index</param>
		/// <param name="propIndex">Targeted (original) tree/prop prop index</param>
		/// <param name="removeEntries">True (default) to remove the reverted entries from the list of replacements, false to leave the list unchanged</param>
		internal void Revert(NetInfo netInfo, PrefabInfo targetInfo, int laneIndex, int propIndex, bool removeEntries) => Revert(EligibileReplacement(netInfo, targetInfo, laneIndex, propIndex), removeEntries);


		/// <summary>
		/// Restores a replacement, if any (e.g. after a higher-priority replacement has been reverted).
		/// </summary>
		/// <param name="netInfo">Network prefab</param>
		/// <param name="targetInfo">Target prop info</param>
		/// <param name="laneIndex">Lane index</param>
		/// <param name="propIndex">Prop index</param>
		/// <returns>True if a restoration was made, false otherwise</returns>
		internal bool Restore(NetInfo netInfo, PrefabInfo targetInfo, int laneIndex, int propIndex)
		{
			// See if we have a relevant replacement record.
			BOBNetReplacement thisReplacement = EligibileReplacement(netInfo, targetInfo, laneIndex, propIndex);
			if (thisReplacement != null)
			{
				// Yes - add reference data to the list.
				NetPropReference newReference = CreateReference(netInfo, laneIndex, propIndex);
				thisReplacement.references.Add(newReference);

				// Apply replacement and return true to indicate restoration.
				ReplaceProp(thisReplacement, newReference);

				return true;
			}

			// If we got here, no restoration was made.
			return false;
		}


		/// <summary>
		/// Unapplies a particular replacement instance to defer to a higher-priority replacement.
		/// </summary>
		/// <param name="netInfo">Network prefab</param>
		/// <param name="targetInfo">Target prefab</param>
		/// <param name="laneIndex">Lane index</param>
		/// <param name="propIndex">Prop index</param>
		internal void RemoveEntry(NetInfo netInfo, PrefabInfo targetInfo, int laneIndex, int propIndex)
		{
			// Check to see if we have an entry for this prefab and target.
			BOBNetReplacement thisReplacement = EligibileReplacement(netInfo, targetInfo, laneIndex, propIndex);
			if (thisReplacement != null)
			{
				NetPropReference thisPropReference = null;

				// Iterate through each recorded prop reference.
				foreach (NetPropReference propReference in thisReplacement.references)
				{
					// Look for a network, lane and index match.
					if (propReference.netInfo == netInfo && propReference.laneIndex == laneIndex && propReference.propIndex == propIndex)
					{
						// Got a match!  Revert instance.
						if (targetInfo is PropInfo propTarget)
						{
							propReference.netInfo.m_lanes[propReference.laneIndex].m_laneProps.m_props[propReference.propIndex].m_finalProp = propTarget;
						}
						else
						{
							propReference.netInfo.m_lanes[propReference.laneIndex].m_laneProps.m_props[propReference.propIndex].m_finalTree = (TreeInfo)targetInfo;
						}
						netInfo.m_lanes[laneIndex].m_laneProps.m_props[propIndex].m_angle = propReference.angle;
						netInfo.m_lanes[laneIndex].m_laneProps.m_props[propIndex].m_position = propReference.position;
						netInfo.m_lanes[laneIndex].m_laneProps.m_props[propIndex].m_probability = propReference.probability;

						// Record the matching reference and stop iterating - we're done here.
						thisPropReference = propReference;
						break;
					}
				}

				// Remove replacement if one was found.
				if (thisPropReference != null)
				{
					thisReplacement.references.Remove(thisPropReference);
				}
			}
		}


		/// <summary>
		/// Checks if there's a currently active replacement applied to the given network, lane and prop index, and if so, returns the replacement record.
		/// </summary>
		/// <param name="netInfo">Net prefab to check</param>
		/// <param name="laneIndex">Lane index to check</param>
		/// <param name="propIndex">Prop index to check</param>
		/// <returns>Replacement record if a replacement is currently applied, null if no replacement is currently applied</returns>
		internal BOBNetReplacement ActiveReplacement(NetInfo netInfo, int laneIndex, int propIndex)
		{
			// See if we've got a replacement entry for this building.
			List<BOBNetReplacement> replacementList = ReplacementList(netInfo);
			if (replacementList == null)
			{
				// No entry - return null.
				return null;
			}

			// Iterate through each replacement record for this network.
			foreach (BOBNetReplacement netReplacement in replacementList)
			{
				// Iterate through each prop reference in this record. 
				foreach (NetPropReference propRef in netReplacement.references)
				{
					// Check for a a network(due to all- replacement), lane and prop index match.
					if (propRef.netInfo == netInfo && propRef.laneIndex == laneIndex && propRef.propIndex == propIndex)
					{
						// Match!  Return the replacement record.
						return netReplacement;
					}
				}
			}

			// If we got here, no entry was found - return null to indicate no active replacement.
			return null;
		}


		/// <summary>
		/// Deserialises a network element list.
		/// </summary>
		/// <param name="elementList">Element list to deserialise</param>
		internal void Deserialize(List<BOBNetworkElement> elementList)
		{
			// Iterate through each element in the provided list.
			foreach (BOBNetworkElement element in elementList)
			{
				// Try to find network prefab.
				element.netInfo = PrefabCollection<NetInfo>.FindLoaded(element.network);

				// Don't bother deserializing further if the network info wasn't found.
				if (element.netInfo != null)
				{
					Deserialize(element.netInfo, element.replacements);
				}
			}
		}


		/// <summary>
		/// Applies a replacement.
		/// </summary>
		/// <param name="replacement">Replacement record to apply</param>
		protected abstract void ApplyReplacement(BOBNetReplacement replacement);


		/// <summary>
		/// Restores any replacements from lower-priority replacements after a reversion.
		/// </summary>
		/// <param name="netInfo">Network prefab</param>
		/// <param name="targetInfo">Target prop info</param>
		/// <param name="laneIndex">Lane index</param>
		/// <param name="propIndex">Prop index</param>
		protected abstract void RestoreLower(NetInfo netInfo, PrefabInfo targetInfo, int laneIndex, int propIndex);


		/// <summary>
		/// Gets the relevant replacement list entry from the active configuration file, if any.
		/// </summary>
		/// <param name="netInfo">Network prefab</param>
		/// <returns>Replacement list for the specified network prefab (null if none)</returns>
		protected virtual List<BOBNetReplacement> ReplacementList(NetInfo netInfo) => NetworkElement(netInfo)?.replacements;


		/// <summary>
		/// Gets the relevant network replacement list entry from the active configuration file, creating a new network entry if none already exists.
		/// </summary>
		/// <param name="netInfo">Network prefab</param>
		/// <returns>Replacement list for the specified network prefab</returns>
		protected virtual List<BOBNetReplacement> ReplacementEntry(NetInfo netInfo)
		{
			// Get existing entry for this network.
			BOBNetworkElement thisNetwork = NetworkElement(netInfo);

			// If no existing entry, create a new one.
			if (thisNetwork == null)
			{
				thisNetwork = new BOBNetworkElement
				{
					network = netInfo.name,
					netInfo = netInfo,
					replacements = new List<BOBNetReplacement>()
				};
				NetworkElementList.Add(thisNetwork);
			}

			// Return replacement list from this entry.
			return thisNetwork.replacements;
		}


		/// <summary>
		/// Reverts a replacement.
		/// </summary>
		/// <param name="replacement">Replacement record to revert</param>
		/// <param name="removeEntries">True to remove the reverted entries from the list of replacements, false to leave the list unchanged</param>
		/// <returns>True if the entire network record was removed from the list (due to no remaining replacements for that prefab), false if the prefab remains in the list (has other active replacements)</returns>
		protected bool Revert(BOBNetReplacement replacement, bool removeEntries)
		{
			// Safety check for calls without any current replacement.
			if (replacement == null)
			{
				return false;
			}

			if (replacement.references != null)
			{
				// Iterate through each entry in our list.
				foreach (NetPropReference propReference in replacement.references)
				{
					// Revert entry.
					if (replacement.tree)
					{
						propReference.netInfo.m_lanes[propReference.laneIndex].m_laneProps.m_props[propReference.propIndex].m_finalTree = replacement.TargetTree;
					}
					else
					{
						propReference.netInfo.m_lanes[propReference.laneIndex].m_laneProps.m_props[propReference.propIndex].m_finalProp = replacement.TargetProp;
					}
					propReference.netInfo.m_lanes[propReference.laneIndex].m_laneProps.m_props[propReference.propIndex].m_angle = propReference.angle;
					propReference.netInfo.m_lanes[propReference.laneIndex].m_laneProps.m_props[propReference.propIndex].m_position = propReference.position;
					propReference.netInfo.m_lanes[propReference.laneIndex].m_laneProps.m_props[propReference.propIndex].m_probability = propReference.probability;

					// Restore any lower-priority replacements.
					RestoreLower(propReference.netInfo, replacement.targetInfo, propReference.laneIndex, propReference.propIndex);

					// Add network to dirty list.
					NetData.DirtyList.Add(propReference.netInfo);
				}

				// Remove entry from list, if we're doing so.
				if (removeEntries)
				{
					// Remove from replacement list.
					ReplacementList(replacement.netInfo).Remove(replacement);

					// See if we've got a parent network element record, and if so, if it has any remaining replacement entries.
					BOBNetworkElement thisElement = NetworkElement(replacement.netInfo);
					if (thisElement != null && (thisElement.replacements == null || thisElement.replacements.Count == 0))
					{
						// No replacement entries left - delete entire network entry and return true to indicate that we've done so.
						NetworkElementList.Remove(thisElement);
						return true;
					}
				}
			}

			// If we got here, we didn't remove any network entries from the list; return false.
			return false;
		}


		/// <summary>
		/// Creates a new PropReference from the provided network prefab, lane and prop index.
		/// </summary>
		/// <param name="netInfo">Network prefab</param>
		/// <param name="laneIndex">Lane index</param>
		/// <param name="propIndex">Prop index</param>
		/// <returns></returns>
		protected NetPropReference CreateReference(NetInfo netInfo, int laneIndex, int propIndex)
		{
			// Safety checks.
			if (netInfo != null || laneIndex < 0 || propIndex < 0)
			{
				// Create and return new reference.
				return new NetPropReference
				{
					netInfo = netInfo,
					laneIndex = laneIndex,
					propIndex = propIndex,
					angle = netInfo.m_lanes[laneIndex].m_laneProps.m_props[propIndex].m_angle,
					position = netInfo.m_lanes[laneIndex].m_laneProps.m_props[propIndex].m_position,
					probability = netInfo.m_lanes[laneIndex].m_laneProps.m_props[propIndex].m_probability
				};
			}

			// If we got here, something went wrong; return null.
			Logging.Error("invalid argument passed to NetworkReplacementBase.CreateReference");
			return null;
		}


		/// <summary>
		/// Replaces a prop, using a network replacement.
		/// </summary>
		/// <param name="replacement">Network replacement to apply</param>
		/// <param name="propReference">Individual prop reference to apply to</param>
		protected void ReplaceProp(BOBNetReplacement replacement, NetPropReference propReference)
		{
			// If this is a vanilla network, then we've probably got shared NetLaneProp references, so need to copy to a new instance.
			// If the name doesn't contain a period (c.f. 12345.MyNetwok_Data), then assume it's vanilla - may be a mod or not shared, but better safe than sorry.
			if (!propReference.netInfo.name.Contains("."))
			{
				CloneLanePropInstance(propReference.netInfo, propReference.laneIndex);
			}

			// Convert offset to Vector3.
			Vector3 offset = new Vector3
			{
				x = replacement.offsetX,
				y = replacement.offsetY,
				z = replacement.offsetZ
			};

			NetInfo.Lane thisLane = propReference.netInfo.m_lanes[propReference.laneIndex];

			// Apply replacement.
			if (replacement.replacementInfo is PropInfo propInfo)
			{
				thisLane.m_laneProps.m_props[propReference.propIndex].m_finalProp = propInfo;
			}
			else if (replacement.replacementInfo is TreeInfo treeInfo)
			{
				thisLane.m_laneProps.m_props[propReference.propIndex].m_finalTree = treeInfo;
			}
			else
			{
				Logging.Error("invalid replacement ", replacement.replacementInfo?.name ?? "null", " passed to NetworkReplacement.ReplaceProp");
			}

			// Invert x offset to match original prop x position.
			if (thisLane.m_position + propReference.position.x < 0)
			{
				offset.x = 0 - offset.x;
			}

			// Angle and offset.
			thisLane.m_laneProps.m_props[propReference.propIndex].m_angle = propReference.angle + replacement.angle;
			thisLane.m_laneProps.m_props[propReference.propIndex].m_position = propReference.position + offset;

			// Probability.
			thisLane.m_laneProps.m_props[propReference.propIndex].m_probability = replacement.probability;

			// Add network to dirty list.
			NetData.DirtyList.Add(propReference.netInfo);
		}


		/// <summary>
		/// Returns the configuration file record for the specified network prefab.
		/// </summary>
		/// <param name="netInfo">Network prefab</param>
		/// <returns>Replacement record for the specified network prefab (null if none)</returns>
		protected BOBNetworkElement NetworkElement(NetInfo netInfo) => netInfo == null ? null : NetworkElementList?.Find(x => x.netInfo == netInfo);


		/// <summary>
		/// Deserialises a replacement list.
		/// </summary>
		/// <param name="netInfo">Network prefab</param>
		/// <param name="elementList">Replacement list to deserialise</param>
		protected void Deserialize(NetInfo NetInfo, List<BOBNetReplacement> replacementList)
		{
			// Iterate through each element in the provided list.
			foreach (BOBNetReplacement replacement in replacementList)
			{
				// Assign network info.
				replacement.netInfo = NetInfo;

				// Try to find target prefab.
				replacement.targetInfo = replacement.tree ? (PrefabInfo)PrefabCollection<TreeInfo>.FindLoaded(replacement.target) : (PrefabInfo)PrefabCollection<PropInfo>.FindLoaded(replacement.target);

				// Try to find replacement prefab.
				replacement.replacementInfo = ConfigurationUtils.FindReplacementPrefab(replacement.Replacement, replacement.tree);

				// Try to apply the replacement.
				ApplyReplacement(replacement);
				Logging.Message("deserialized network replacement ", replacement.netInfo?.name ?? "null", " with target ", replacement.targetInfo?.name ?? "null", " and replacement ", replacement.replacementInfo?.name ?? "null");
			}
		}


		/// <summary>
		/// Creates a new NetInfo.Lane instance for the specified network and lane index.
		/// Used to 'separate' target networks for individual and network prop replacement when the network uses shared m_laneProps (e.g. vanilla roads).
		/// </summary>
		/// <param name="network">Network prefab</param>
		/// <param name="lane">Lane index</param>
		private void CloneLanePropInstance(NetInfo network, int lane)
		{
			// Don't do anything if we've previously converted this one.
			if (network.m_lanes[lane].m_laneProps is NewNetLaneProps)
			{
				return;
			}

			Logging.Message("creating new m_laneProps instance for network ", network.name, " at lane ", lane.ToString());

			// Create new m_laneProps instance with new props list, using our custom class instead of NetLaneProps as a flag that we've already done this one.
			NewNetLaneProps newLaneProps = ScriptableObject.CreateInstance<NewNetLaneProps>();
			newLaneProps.m_props = new NetLaneProps.Prop[network.m_lanes[lane].m_laneProps.m_props.Length];

			// Iterate through each  in the existing instance
			for (int i = 0; i < newLaneProps.m_props.Length; ++i)
			{
				NetLaneProps.Prop existingNetLaneProp = network.m_lanes[lane].m_laneProps.m_props[i];

				newLaneProps.m_props[i] = new NetLaneProps.Prop
				{
					m_flagsRequired = existingNetLaneProp.m_flagsRequired,
					m_flagsForbidden = existingNetLaneProp.m_flagsForbidden,
					m_startFlagsRequired = existingNetLaneProp.m_startFlagsRequired,
					m_startFlagsForbidden = existingNetLaneProp.m_startFlagsForbidden,
					m_endFlagsRequired = existingNetLaneProp.m_endFlagsRequired,
					m_endFlagsForbidden = existingNetLaneProp.m_endFlagsForbidden,
					m_colorMode = existingNetLaneProp.m_colorMode,
					m_prop = existingNetLaneProp.m_prop,
					m_tree = existingNetLaneProp.m_tree,
					m_position = existingNetLaneProp.m_position,
					m_angle = existingNetLaneProp.m_angle,
					m_segmentOffset = existingNetLaneProp.m_segmentOffset,
					m_repeatDistance = existingNetLaneProp.m_repeatDistance,
					m_minLength = existingNetLaneProp.m_minLength,
					m_cornerAngle = existingNetLaneProp.m_cornerAngle,
					m_probability = existingNetLaneProp.m_probability,
					m_finalProp = existingNetLaneProp.m_finalProp,
					m_finalTree = existingNetLaneProp.m_finalTree
				};
			}

			// Replace network laneProps with our new instance.
			network.m_lanes[lane].m_laneProps = newLaneProps;
		}
	}


	/// <summary>
	/// 'Dummy' class for new NetLaneProps.Prop instances to overcome network NetLaneProps sharing.
	/// </summary>
	public class NewNetLaneProps : NetLaneProps
	{
    }
}
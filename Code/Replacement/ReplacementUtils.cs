﻿using UnityEngine;


namespace BOB
{
	/// <summary>
	/// Static class for replacment record management utilities.
	/// </summary>
	internal static class ReplacementUtils
	{
		/// <summary>
		/// Creates a clone of the given replacement record.
		/// </summary>
		/// <param name="replacement">Record to clone</param>
		/// <returns>Clone of the original record</returns>
		internal static Replacement Clone(Replacement replacement)
		{
			// Create new record instance.
			Replacement clone = new Replacement();

			// Copy original records to the clone.
			clone.isTree = replacement.isTree;
			clone.targetIndex = replacement.targetIndex;
			clone.targetName = replacement.targetName;
			clone.replaceName = replacement.replaceName;
			clone.probability = replacement.probability;
			clone.angle = replacement.angle;
			clone.replacementInfo = replacement.replacementInfo;
			clone.targetInfo = replacement.targetInfo;

			return clone;
		}


		/// <summary>
		/// Creates a clone of the given net replacement record.
		/// </summary>
		/// <param name="replacement">Record to clone</param>
		/// <returns>Clone of the original record</returns>
		internal static NetReplacement Clone(NetReplacement replacement)
		{
			// Create new record instance.
			NetReplacement clone = new NetReplacement();

			// Copy original records to the clone.
			clone.isTree = replacement.isTree;
			clone.targetIndex = replacement.targetIndex;
			clone.targetName = replacement.targetName;
			clone.replaceName = replacement.replaceName;
			clone.probability = replacement.probability;
			clone.angle = replacement.angle;
			clone.replacementInfo = replacement.replacementInfo;
			clone.targetInfo = replacement.targetInfo;
			clone.lane = replacement.lane;
			clone.offsetX = replacement.offsetX;
			clone.offsetY = replacement.offsetY;
			clone.offsetZ = replacement.offsetZ;

			return clone;
		}
	}
}

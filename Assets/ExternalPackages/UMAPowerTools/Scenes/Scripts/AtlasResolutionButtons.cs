using UnityEngine;
using System.Collections.Generic;
using UMA;

namespace UMA.PowerTools
{
	public class AtlasResolutionButtons : MonoBehaviour 
	{
		public HashSet<UMA.UMAData> avatars;
		public UnityEngine.UI.Slider slider;
	
		public UMAGeneratorBase umaGenerator;
		public UMAGeneratorBase umaGeneratorDefault;
		public UMAGeneratorBase umaGeneratorThreaded;
		public UnityEngine.UI.Toggle toggle;
		public System.Diagnostics.Stopwatch sw;
		public int startFrame;
	
		public void ToggleGenerator(bool threaded)
		{
			if (!umaGenerator.IsIdle())
			{
				toggle.isOn = umaGenerator == umaGeneratorThreaded;
				return;
			}
			umaGenerator = threaded ? umaGeneratorThreaded : umaGeneratorDefault;
			umaGeneratorDefault.gameObject.SetActive(!threaded);
			umaGeneratorThreaded.gameObject.SetActive(threaded);
			slider.maxValue = avatars.Count;
			slider.value = 0;
			foreach (var avatar in avatars) 
			{
				var dynAvatar = avatar.GetComponent<UMADynamicAvatar>();
				dynAvatar.Hide();
				dynAvatar.umaGenerator = umaGenerator;
				avatar.umaGenerator = umaGenerator;
				dynAvatar.UpdateNewRace();
			}
			if (sw == null) sw = new System.Diagnostics.Stopwatch();
			sw.Reset();
			sw.Start();
			startFrame = Time.frameCount;
		}
	
		public void DoubleAtlasResolution()
		{
			if (!umaGenerator.IsIdle()) return;
			if (avatars == null) return;
			slider.maxValue = avatars.Count;
			slider.value = 0;
			foreach (var avatar in avatars)
			{
				if (avatar.atlasResolutionScale < 1f)
				{
					avatar.atlasResolutionScale = Mathf.Min(1f, avatar.atlasResolutionScale * 2f);
					avatar.Dirty(false, true, false);
				}
			}
			if (sw == null) sw = new System.Diagnostics.Stopwatch();
			sw.Reset();
			sw.Start();
			startFrame = Time.frameCount;
		}
		public void HalfAtlasResolution()
		{
			if (!umaGenerator.IsIdle()) return;
			if (avatars == null) return;
			slider.maxValue = avatars.Count;
			slider.value = 0;
			foreach (var avatar in avatars)
			{
				avatar.atlasResolutionScale = avatar.atlasResolutionScale * 0.5f;
				avatar.Dirty(false, true, false);
			}
			if (sw == null) sw = new System.Diagnostics.Stopwatch();
			sw.Reset();
			sw.Start();
			startFrame = Time.frameCount;
		}
	
		public void AvatarCreated(UMA.UMAData umaData)
		{
			if (avatars == null) avatars = new HashSet<UMA.UMAData>();
			avatars.Add(umaData);
		}
	
		public void AvatarUpdated(UMA.UMAData umaData)
		{
			if (slider.value < slider.maxValue)
			{
				slider.value += 1;
				if (slider.value == slider.maxValue && sw != null)
				{
					sw.Stop();
					Debug.Log(string.Format("{0} frames and {1} ms", Time.frameCount - startFrame, sw.ElapsedMilliseconds));
				}
			}
			else
			{
				slider.value = 0;
				slider.maxValue = umaGenerator.QueueSize();
			}
		}
	
		public void AvatarDestroyed(UMA.UMAData umaData)
		{
			if (avatars == null) return;
			avatars.Remove(umaData);
		}
	}
}
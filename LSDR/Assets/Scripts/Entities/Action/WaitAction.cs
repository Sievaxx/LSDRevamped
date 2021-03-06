﻿using UnityEngine;
using System.Collections;
using LSDR.Types;
using LSDR.Util;
using LSDR.Entities.Dream;

namespace LSDR.Entities.Action
{
	// TODO: WaitAction is obsolete
	public class WaitAction : BaseAction
	{
		public float WaitTime;

		public static GameObject Instantiate(ENTITY e)
		{
			GameObject instantiated = new GameObject(e.Classname);
			WaitAction actionScript = instantiated.AddComponent<WaitAction>();
			actionScript.Name = e.GetPropertyValue("Sequence name");
			actionScript.SequencePosition = EntityUtil.TryParseInt("Sequence position", e);

			actionScript.WaitTime = EntityUtil.TryParseFloat("Wait time", e);

			DreamDirector.PostLoadEvent += actionScript.PostLoad;

			EntityUtil.SetInstantiatedObjectTransform(e, ref instantiated);

			return instantiated;
		}

		private void PostLoad()
		{
			ReferencedSequence = ActionSequence.FindSequence(Name);
			AddSelf();
		}

		public override IEnumerator DoAction()
		{
			yield return new WaitForSeconds(WaitTime);
			ReferencedSequence.DoNextAction();
		}
	}
}
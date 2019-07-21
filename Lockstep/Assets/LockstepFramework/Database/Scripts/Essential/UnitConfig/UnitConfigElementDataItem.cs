﻿using UnityEngine;
using System;

namespace Lockstep.Data
{
	[Serializable]
	public class UnitConfigElementDataItem : Lockstep.Data.DataItem
	{
		[SerializeField, TypeReferences.ClassExtends(typeof(Ability))]

		private TypeReferences.ClassTypeReference _componentType;
		public Type ComponentType
		{
			get
			{
				return _componentType.Type;
			}
		}

		[SerializeField] private string _field;
		public string Field { get { return _field; } }
	}
}
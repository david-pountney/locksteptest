﻿using UnityEngine;

public static class NDRaycast
{
	public static bool Raycast(Ray ray, out RaycastHit hit)
	{
		return Physics.Raycast(ray, out hit);
	}
}

﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Entities.WorldObject;
using IO;
using MapParse;
using MapParse.Types;
using SimpleJSON;
using Types;
using UnityEngine;
using Util;

namespace Game
{
	public static class ResourceManager
	{
		private static Dictionary<string, GenericResource> _resources = new Dictionary<string, GenericResource>();

		private static GameObject _resourceManagerGameObject = GameObject.FindGameObjectWithTag("ResourceManager");

		// TODO: handle missing files

		public static DataType Load<DataType>(string filePath, ResourceLifespan lifespan = ResourceLifespan.GLOBAL, bool absolutePath = false)
		{
			string normalizedPath = IOUtil.NormalizePath(absolutePath ? filePath : IOUtil.PathCombine(Application.dataPath, filePath));
			
			// if the cache contains the resource, return it
			if (_resources.ContainsKey(normalizedPath))
			{
				return ((GenericResource<DataType>)_resources[normalizedPath]).Resource;
			}

			string fileExt = Path.GetExtension(normalizedPath);

			switch (fileExt)
			{
				case ".png":
				{
					GenericResource<Texture2D> resource = new GenericResource<Texture2D>();
					resource.Resource = IOUtil.LoadPNG(normalizedPath);
					resource.Lifespan = lifespan;
					_resources.Add(normalizedPath, resource);
					break;
				}
				case ".json":
				{
					GenericResource<JSONClass> resource = new GenericResource<JSONClass>();
					resource.Resource = IOUtil.ReadJSONFromDisk(normalizedPath);
					resource.Lifespan = lifespan;
					_resources.Add(normalizedPath, resource);
					break;
				}
				case ".map":
				{
					GenericResource<GameObject> resource = new GenericResource<GameObject>();
					resource.Resource = MapReader.LoadMap(normalizedPath, IOUtil.PathCombine(Application.dataPath, "textures", "wad"),
						Shader.Find(GameSettings.UseClassicShaders ? "LSD/PSX/DiffuseSetNoAffine" : "LSD/DiffuseSet"),
						Shader.Find(GameSettings.UseClassicShaders ? "LSD/PSX/TransparentSetNoAffine" : "LSD/TransparentSet"));
					resource.Resource.transform.SetParent(_resourceManagerGameObject.transform);
					resource.Resource.SetActive(false);
					resource.Lifespan = lifespan;
					_resources.Add(normalizedPath, resource);
					break;
				}
				case ".tmap":
				{
					GenericResource<TMAP> resource = new GenericResource<TMAP>();
					resource.Resource = ToriiMapReader.ReadFromFile(normalizedPath);
					resource.Lifespan = lifespan;
					_resources.Add(normalizedPath, resource);
					break;
				}
				case ".tobj":
				{
					GenericResource<GameObject> resource = new GenericResource<GameObject>();
					TOBJ tobj = new TOBJ();
					ToriiObjectReader.Read(normalizedPath, ref tobj);
					resource.Resource = OBJReader.ReadOBJString(tobj.ObjectFile);
					resource.Resource.AddComponent<ToriiObject>();
					ToriiObject toriiObjScript = resource.Resource.GetComponent<ToriiObject>();
					toriiObjScript.ToriiObj = tobj;
					resource.Resource.transform.SetParent(_resourceManagerGameObject.transform);
					resource.Resource.SetActive(false);
					resource.Lifespan = lifespan;
					_resources.Add(normalizedPath, resource);
					break;
				}
				default:
				{
					throw new ResourceLoadException("Did not recognize file type: " + normalizedPath);
				}
			}

			return ((GenericResource<DataType>) _resources[normalizedPath]).Resource;
		}

		public static void ClearLifespan(ResourceLifespan lifespan)
		{
			string[] keys = _resources.Keys.ToArray();
			foreach (string resource in keys)
			{
				if (_resources[resource].Lifespan == lifespan)
				{
					string extension = Path.GetExtension(resource);
					// if there's a gameobject, destroy it
					if (extension == ".tobj" || extension == ".map") UnityEngine.Object.Destroy(((GenericResource<GameObject>)_resources[resource]).Resource);
					_resources.Remove(resource);
				}
			}
		}

		private abstract class GenericResource
		{
			public ResourceLifespan Lifespan;
		}
		private class GenericResource<T> : GenericResource
		{
			public T Resource;
		}

		private class ResourceLoadException : Exception
		{
			public ResourceLoadException() { }
			public ResourceLoadException(string message) : base(message) { }
			public ResourceLoadException(string message, Exception inner) : base(message, inner) { }
		}
	}

	public enum ResourceLifespan
	{
		LEVEL,
		DREAM,
		GLOBAL
	}
}
using System.Collections.Generic;
using UMA;
using UMAEditor;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace UMACharacterSystem
{
    [CustomEditor(typeof(UMAAssetIndexer))]
	public class UMAAssetIndexerEditor : Editor 
	{
		Dictionary<System.Type, bool> Toggles = new Dictionary<System.Type, bool>();
		UMAAssetIndexer UAI;
		List<Object> AddedDuringGui = new List<Object>();
		List<System.Type> AddedTypes = new List<System.Type>();
		List<AssetItem> DeletedDuringGUI = new List<AssetItem>();
		List<System.Type> RemovedTypes = new List<System.Type>();
		public string Filter = "";
        public bool IncludeText;
        int NotInBuildCount = 0;

		public UMAMaterial SelectedMaterial = null;

		[MenuItem("UMA/UMA Global Library")]
		public static void Init()
		{
			UMAAssetIndexer ua = UMAAssetIndexer.Instance;
			Selection.activeObject = ua.gameObject;
		}

		#region Drag Drop
		private void DropAreaGUI(Rect dropArea)
		{

			var evt = Event.current;

			if (evt.type == EventType.DragUpdated)
			{
				if (dropArea.Contains(evt.mousePosition))
				{
					DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
				}
			}

			if (evt.type == EventType.DragPerform)
			{
				if (dropArea.Contains(evt.mousePosition))
				{
					DragAndDrop.AcceptDrag();
					AddedDuringGui.Clear();
					UnityEngine.Object[] draggedObjects = DragAndDrop.objectReferences as UnityEngine.Object[];
					for (int i = 0; i < draggedObjects.Length; i++)
					{
						if (draggedObjects[i])
						{
							AddedDuringGui.Add(draggedObjects[i]);

							var path = AssetDatabase.GetAssetPath(draggedObjects[i]);
							if (System.IO.Directory.Exists(path))
							{
								RecursiveScanFoldersForAssets(path);
							}
						}
					}
				}
			}
		}

		private void DropAreaType(Rect dropArea)
		{

			var evt = Event.current;

			if (evt.type == EventType.DragUpdated)
			{
				if (dropArea.Contains(evt.mousePosition))
				{
					DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
				}
			}

			if (evt.type == EventType.DragPerform)
			{
				if (dropArea.Contains(evt.mousePosition))
				{
					DragAndDrop.AcceptDrag();
					AddedTypes.Clear();
					UnityEngine.Object[] draggedObjects = DragAndDrop.objectReferences as UnityEngine.Object[];
					for (int i = 0; i < draggedObjects.Length; i++)
					{
						if (draggedObjects[i])
						{
							System.Type sType = draggedObjects[i].GetType();

							AddedTypes.Add(sType);
						}
					}
				}
			}
		}

		private void AddObject(Object draggedObject)
		{
			System.Type type = draggedObject.GetType();
			if (UAI.IsIndexedType(type))
			{
				UAI.EvilAddAsset(type, draggedObject);
			}
		}

		private void RecursiveScanFoldersForAssets(string path)
		{
			var assetFiles = System.IO.Directory.GetFiles(path);

			foreach (var assetFile in assetFiles)
			{
				string Extension = System.IO.Path.GetExtension(assetFile).ToLower();
				if (Extension == ".asset" || Extension == ".controller" || Extension == ".txt")
				{
					Object o = AssetDatabase.LoadMainAssetAtPath(assetFile);

					if (o)
					{
						AddedDuringGui.Add(o);
					}
				}
			}
			foreach (var subFolder in System.IO.Directory.GetDirectories(path))
			{
				RecursiveScanFoldersForAssets(subFolder.Replace('\\', '/'));
			}
		}
		#endregion

		private void Cleanup()
		{
			if (AddedDuringGui.Count > 0 || DeletedDuringGUI.Count > 0 || AddedTypes.Count > 0 || RemovedTypes.Count > 0)
			{
				foreach (Object o in AddedDuringGui)
				{
					AddObject(o);
				}

				foreach (AssetItem ai in DeletedDuringGUI)
				{
					UAI.RemoveAsset(ai._Type, ai._Name);
				}

				foreach (System.Type st in RemovedTypes)
				{
					UAI.RemoveType(st);
				}

				foreach (System.Type st in AddedTypes)
				{
					UAI.AddType(st);
				}

				AddedTypes.Clear();
				RemovedTypes.Clear();
				DeletedDuringGUI.Clear();
				AddedDuringGui.Clear();

				UAI.ForceSave();
				Repaint();
			}
		}

		private void SetFoldouts(bool Value)
		{
			System.Type[] Types = UAI.GetTypes();
			foreach (System.Type t in Types)
			{
				Toggles[t] = Value;
			}
		}

		public override void OnInspectorGUI()
		{
			UAI = target as UMAAssetIndexer;

			if (Event.current.type == EventType.Layout)
			{
				Cleanup();
			}

			ShowTypes();

			// Draw and handle the Drag/Drop
			GUILayout.Space(20);
			Rect dropArea = GUILayoutUtility.GetRect(0.0f, 50.0f, GUILayout.ExpandWidth(true));
			GUI.Box(dropArea, "Drag Indexable Assets here. Non indexed assets will be ignored.");
			GUILayout.Space(20);
			DropAreaGUI(dropArea);

			System.Type[] Types = UAI.GetTypes();
			if (Toggles.Count != Types.Length) SetFoldouts(false);

			GUILayout.BeginHorizontal();
			if (GUILayout.Button("Reindex Names"))
			{
				UAI.RebuildIndex();
			}
            if (GUILayout.Button("Add Build References"))
            {
                UAI.AddReferences();
                Resources.UnloadUnusedAssets();
            }
            GUILayout.EndHorizontal();


            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Rebuild From Project (Adds Everything)"))
            {
                UAI.Clear();
                UAI.AddEverything(IncludeText);
                Resources.UnloadUnusedAssets();
            }
            IncludeText = GUILayout.Toggle(IncludeText, "Include TextAssets");
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Clear References"))
			{
				UAI.ClearReferences();
				Resources.UnloadUnusedAssets();
			}
			if (GUILayout.Button("Clear All"))
			{
				UAI.Clear();
			}
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			if (GUILayout.Button("Collapse All"))
			{
				SetFoldouts(false);
			}

			if (GUILayout.Button("Expand All"))
			{
				SetFoldouts(true);
			}

			GUILayout.EndHorizontal();

			//bool PreSerialize = UAI.SerializeAllObjects;
			////UAI.SerializeAllObjects = EditorGUILayout.Toggle("Serialize for build (SLOW)", UAI.SerializeAllObjects);
			//if (UAI.SerializeAllObjects != PreSerialize)
			//{
			//	UAI.ForceSave();
			//}
			UAI.AutoUpdate = EditorGUILayout.Toggle("Process Updates", UAI.AutoUpdate);
            GUILayout.BeginHorizontal();

            Filter = EditorGUILayout.TextField("Filter Library", Filter);
            GUI.SetNextControlName("TheDumbUnityBuggyField");
            if (GUILayout.Button("-", GUILayout.Width(20)))
            {
                Filter = "";
                // 10 year old unity bug that no one wants to fix.
                GUI.FocusControl("TheDumbUnityBuggyField");
            }
            GUILayout.EndHorizontal();

            bool HasErrors = false;
            string ErrorTypes = "";
            NotInBuildCount = 0;

            foreach (System.Type t in Types)
			{
				if (t != typeof(AnimatorController)) // Somewhere, a kitten died because I typed that.
				{
					if (ShowArray(t, Filter))
                    {
                        HasErrors = true;
                        ErrorTypes += t.Name +" ";
                    }
				}
			}
            string bldMessage = "(" + NotInBuildCount + ") Item(s) not in build";
            GUILayout.BeginHorizontal();
            if (HasErrors)
            {
                GUILayout.Label(ErrorTypes + "Have error items! "+bldMessage);
            }
            else
            {
                GUILayout.Label("All items appear OK. " +bldMessage);
            }
            GUILayout.EndHorizontal();
		}


		public bool ShowArray(System.Type CurrentType, string Filter)
		{
			bool HasFilter = false;
            bool NotFound = false;
            string actFilter = Filter.Trim().ToLower();
			if (actFilter.Length > 0)
				HasFilter = true;
			Dictionary<string, AssetItem> TypeDic = UAI.GetAssetDictionary(CurrentType);

            List<AssetItem> Items = new List<AssetItem>();
            Items.AddRange(TypeDic.Values);

            int NotInBuild = 0;
            int VisibleItems = 0;
            foreach (AssetItem ai in Items)
            {
                if (ai._SerializedItem == null)
                {
                    NotInBuild++;
                }
                string Displayed = ai.ToString(UMAAssetIndexer.SortOrder);
                if (HasFilter && (!Displayed.ToLower().Contains(actFilter)))
                {
                    continue;
                }
                VisibleItems++;
            }

            NotInBuildCount += NotInBuild;
            GUILayout.BeginHorizontal(EditorStyles.toolbarButton);
			GUILayout.Space(10);
			Toggles[CurrentType] = EditorGUILayout.Foldout(Toggles[CurrentType], CurrentType.Name + ":  "+VisibleItems+ "/" + TypeDic.Count + " Item(s). "+NotInBuild+" Not in build.");
			GUILayout.EndHorizontal();



			if (Toggles[CurrentType])
			{
                Items.Sort();
                GUIHelper.BeginVerticalPadded(5, new Color(0.75f, 0.875f, 1f));
				GUILayout.BeginHorizontal();
				GUILayout.Label("Sorted By: " + UMAAssetIndexer.SortOrder, GUILayout.MaxWidth(160));
				foreach (string s in UMAAssetIndexer.SortOrders)
				{
					if (GUILayout.Button(s, GUILayout.Width(80)))
					{
						UMAAssetIndexer.SortOrder = s;
					}
				}
				GUILayout.EndHorizontal();


                foreach (AssetItem ai in Items)
                {
                    string lblBuild = "B-";
					string lblVal = ai.ToString(UMAAssetIndexer.SortOrder);
					if (HasFilter && (!lblVal.ToLower().Contains(actFilter)))
						continue;

                    if (ai._Name == "< Not Found!>")
                    {
                        NotFound = true;
                    }
					GUILayout.BeginHorizontal(EditorStyles.textField);

                    if (ai._SerializedItem == null)
                    {
                        lblVal += "<Not in Build>";
                        lblBuild = "B+";
                    }

					if (GUILayout.Button(lblVal /* ai._Name + " (" + ai._AssetBaseName + ")" */, EditorStyles.label))
					{
						EditorGUIUtility.PingObject(AssetDatabase.LoadMainAssetAtPath(ai._Path));
					}

                    if (GUILayout.Button(lblBuild,GUILayout.Width(35)))
                    {
                        if (ai._SerializedItem == null)
                        {
                            // Force the item to load.
                            Object o = ai.Item; 
                        }
                        else
                        {
                            ai.ReleaseItem();
                        }

                    }
					if (GUILayout.Button("-", GUILayout.Width(20.0f)))
					{
						DeletedDuringGUI.Add(ai);
					}
					GUILayout.EndHorizontal();
				}

                GUILayout.BeginHorizontal();
                if (NotFound)
                {
                    GUILayout.Label("Warning - Some items not found!");
                }
                else
                {
                    GUILayout.Label("All Items appear OK");
                }
                GUILayout.EndHorizontal();
				if (CurrentType == typeof(SlotDataAsset) || CurrentType == typeof(OverlayDataAsset))
				{
					GUIHelper.BeginVerticalPadded(5, new Color(0.65f, 0.65f, 0.65f));
					GUILayout.Label("Utilities");
					GUILayout.Space(10);
					EditorGUILayout.BeginHorizontal();
					SelectedMaterial = (UMAMaterial)EditorGUILayout.ObjectField(SelectedMaterial, typeof(UMAMaterial), false);
					GUILayout.Label("Assign To");
					if (GUILayout.Button("Unassigned"))
					{
						foreach (AssetItem ai in Items)
						{
							string lblVal = ai.ToString(UMAAssetIndexer.SortOrder);
							if (HasFilter && (!lblVal.ToLower().Contains(actFilter)))
								continue;

							if (ai._Type == typeof(SlotDataAsset))
							{
								if ((ai.Item as SlotDataAsset).material != null) continue;
								(ai.Item as SlotDataAsset).material = SelectedMaterial;
							}
							if (ai._Type == typeof(OverlayDataAsset))
							{
								if ((ai.Item as OverlayDataAsset).material != null) continue;
								(ai.Item as OverlayDataAsset).material = SelectedMaterial;
							}
						}
					}
					if (GUILayout.Button("All"))
					{
						foreach (AssetItem ai in Items)
						{
							string lblVal = ai.ToString(UMAAssetIndexer.SortOrder);
							if (HasFilter && (!lblVal.ToLower().Contains(actFilter)))
								continue;

							if (ai._Type == typeof(SlotDataAsset))
							{
								(ai.Item as SlotDataAsset).material = SelectedMaterial;
							}
							if (ai._Type == typeof(OverlayDataAsset))
							{
								(ai.Item as OverlayDataAsset).material = SelectedMaterial;
							}
						}
					}

					EditorGUILayout.EndHorizontal();
					GUIHelper.EndVerticalPadded(5);
				}

				GUIHelper.EndVerticalPadded(5);
			}
            return NotFound;
		}

		public bool bShowTypes;

		public void ShowTypes()
		{
			GUILayout.BeginHorizontal(EditorStyles.toolbarButton);
			GUILayout.Space(10);
			bShowTypes = EditorGUILayout.Foldout(bShowTypes, "Additional Indexed Types");
			GUILayout.EndHorizontal();

			if (bShowTypes)
			{
				GUIHelper.BeginVerticalPadded(10, new Color(0.75f, 0.875f, 1f));

				// Draw and handle the Drag/Drop
				GUILayout.Space(20);
				Rect dropTypeArea = GUILayoutUtility.GetRect(0.0f, 50.0f, GUILayout.ExpandWidth(true));
				GUI.Box(dropTypeArea, "Drag a single type here to start indexing that type.");
				GUILayout.Space(20);
				DropAreaType(dropTypeArea);
				foreach (string s in UAI.IndexedTypeNames)
				{
					System.Type CurrentType = System.Type.GetType(s);
					GUILayout.BeginHorizontal(EditorStyles.textField);
					GUILayout.Label(CurrentType.ToString(), GUILayout.MinWidth(240));
					if (GUILayout.Button("-", GUILayout.Width(20.0f)))
					{
						RemovedTypes.Add(CurrentType);
					}
					GUILayout.EndHorizontal();
				}
				GUIHelper.EndVerticalPadded(10);
			}
		}
	}
}

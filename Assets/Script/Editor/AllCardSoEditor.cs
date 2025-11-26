using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Script.Editor
{
    [CustomEditor(typeof(AllCardSo))]
    public class AllCardSoEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            AllCardSo allCardSo = (AllCardSo)target;

            // Draw the default inspector
            DrawDefaultInspector();

            EditorGUILayout.Space(20);
            
            // Add a separator line
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            
            EditorGUILayout.Space(10);

            // Style for the button
            GUIStyle buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                fixedHeight = 40
            };

            // Refresh All Cards button
            GUI.backgroundColor = new Color(0.4f, 0.8f, 0.4f); // Green color
            if (GUILayout.Button("🔄 Refresh All Cards", buttonStyle))
            {
                RefreshAllCards(allCardSo);
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.Space(10);
            
            // Show current card count
            EditorGUILayout.HelpBox($"Current card count: {allCardSo.allCardData.Count}", MessageType.Info);
        }

        private void RefreshAllCards(AllCardSo allCardSo)
        {
            // Find all CardDataSo assets in the project
            string[] guids = AssetDatabase.FindAssets("t:CardDataSo");
            
            List<CardDataSo> foundCards = new List<CardDataSo>();

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                CardDataSo cardData = AssetDatabase.LoadAssetAtPath<CardDataSo>(path);
                
                if (cardData != null)
                {
                    foundCards.Add(cardData);
                }
            }

            // Sort by type name for better organization
            foundCards = foundCards.OrderBy(c => c.type).ToList();

            // Record undo
            Undo.RecordObject(allCardSo, "Refresh All Cards");

            // Clear and add all found cards
            allCardSo.allCardData.Clear();
            allCardSo.allCardData.AddRange(foundCards);

            // Mark as dirty so changes are saved
            EditorUtility.SetDirty(allCardSo);
            AssetDatabase.SaveAssets();

            Debug.Log($"[AllCardSo] Refreshed! Found {foundCards.Count} cards.");
            
            // Show a dialog with the result
            EditorUtility.DisplayDialog(
                "Refresh Complete", 
                $"Found and added {foundCards.Count} card(s) to the list.\n\nCards:\n{string.Join("\n", foundCards.Select(c => "• " + c.type))}", 
                "OK"
            );
        }
    }
}


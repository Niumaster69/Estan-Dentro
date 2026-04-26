using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using EstanDentro.UI;

namespace EstanDentro.Inventory
{
    public class Inventory : MonoBehaviour
    {
        public static Inventory Instance { get; private set; }

        [System.Serializable]
        public class NoteEntry
        {
            public string title;
            public string body;
            public NoteEntry(string title, string body) { this.title = title; this.body = body; }
        }

        public List<NoteEntry> ReadNotes { get; private set; } = new List<NoteEntry>();
        public bool HasAny => ReadNotes.Count > 0;
        public int Count => ReadNotes.Count;

        public event Action OnInventoryChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
        }

        private void OnDestroy() { if (Instance == this) Instance = null; }

        public void RegisterNote(string title, string body)
        {
            // Evita duplicados (por title)
            foreach (var n in ReadNotes)
                if (n.title == title) return;
            ReadNotes.Add(new NoteEntry(title, body));
            OnInventoryChanged?.Invoke();
            Debug.Log($"[Inventory] Nota leida: '{title}'. Total: {ReadNotes.Count}");
        }

        public void Clear()
        {
            ReadNotes.Clear();
            OnInventoryChanged?.Invoke();
        }

        private void Update()
        {
            // No abrir si hay algun overlay modal activo
            if (OverlayBlocker.IsBlocking) return;
            if (Time.timeScale <= 0f) return;

            var kb = Keyboard.current;
            var gp = Gamepad.current;
            bool hit = (kb != null && kb.iKey.wasPressedThisFrame)
                    || (gp != null && gp.selectButton.wasPressedThisFrame);
            if (hit) InventoryOverlay.Open();
        }
    }
}

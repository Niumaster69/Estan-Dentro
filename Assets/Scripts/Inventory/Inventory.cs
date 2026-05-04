using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using EstanDentro.UI;
using EstanDentro.Network;

namespace EstanDentro.Inventory
{
    public class Inventory : MonoBehaviour
    {
        public static Inventory Instance { get; private set; }

        // Cantidad de notas distintas necesarias para desbloquear 'notas_completas'.
        // Si en el futuro hay mas notas pedagogicas en el capitulo, subir este numero.
        private const int NotasParaLogroCompletas = 3;

        [System.Serializable]
        public class NoteEntry
        {
            public string title;
            public string body;
            public NoteEntry(string title, string body) { this.title = title; this.body = body; }
        }

        [System.Serializable]
        public class ItemEntry
        {
            public string id;          // identificador estable, ej. "linterna", "destornillador"
            public string displayName; // texto para mostrar al jugador
            public ItemEntry(string id, string name) { this.id = id; this.displayName = name; }
        }

        public enum MissionCategory { Principal, Secundaria }

        [System.Serializable]
        public class MissionEntry
        {
            public string id;
            public string text;
            public MissionCategory category;
            public bool completed;
            public MissionEntry(string id, string text, MissionCategory cat)
            {
                this.id = id; this.text = text; this.category = cat; this.completed = false;
            }
        }

        public List<NoteEntry> ReadNotes { get; private set; } = new List<NoteEntry>();
        public List<ItemEntry> CarriedItems { get; private set; } = new List<ItemEntry>();
        public List<MissionEntry> Missions { get; private set; } = new List<MissionEntry>();
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
            // Evita duplicados (por title). Si dos notas tienen el mismo title, la segunda
            // se descarta. Si esto pasa, probablemente olvidaste setear noteTitle/noteText
            // en el Inspector de uno de los GameObjects de Note.
            foreach (var n in ReadNotes)
            {
                if (n.title == title)
                {
                    Debug.LogWarning($"[Inventory] Nota '{title}' ya estaba registrada — DESCARTADA. " +
                                     $"Si esto no es lo esperado, revisa que cada GameObject de Note tenga un title unico en su Inspector.");
                    return;
                }
            }
            ReadNotes.Add(new NoteEntry(title, body));
            OnInventoryChanged?.Invoke();
            Debug.Log($"[Inventory] Nota leida: '{title}'. Total: {ReadNotes.Count}");

            // Logro 'notas_completas': leer al menos N notas distintas en la partida
            if (ReadNotes.Count >= NotasParaLogroCompletas)
                GameSession.TryUnlockLogro("notas_completas");
        }

        // ----- Items (linterna, destornillador, etc) -----

        public void RegisterItem(string id, string displayName = null)
        {
            if (string.IsNullOrEmpty(id)) return;
            foreach (var it in CarriedItems) if (it.id == id) return; // dedupe
            CarriedItems.Add(new ItemEntry(id, displayName ?? id));
            OnInventoryChanged?.Invoke();
            Debug.Log($"[Inventory] Item recogido: '{id}'. Total items: {CarriedItems.Count}");
        }

        public bool HasItem(string id)
        {
            if (string.IsNullOrEmpty(id)) return false;
            foreach (var it in CarriedItems) if (it.id == id) return true;
            return false;
        }

        public void RemoveItem(string id)
        {
            for (int i = 0; i < CarriedItems.Count; i++)
            {
                if (CarriedItems[i].id == id)
                {
                    CarriedItems.RemoveAt(i);
                    OnInventoryChanged?.Invoke();
                    return;
                }
            }
        }

        // ----- Misiones (objetivos del puzzle) -----

        public void AddMission(string id, string text, MissionCategory category = MissionCategory.Principal)
        {
            if (string.IsNullOrEmpty(id)) return;
            // Evitar duplicados; si existe, solo actualiza texto
            foreach (var m in Missions)
            {
                if (m.id == id) { m.text = text; m.category = category; m.completed = false; OnInventoryChanged?.Invoke(); return; }
            }
            Missions.Add(new MissionEntry(id, text, category));
            OnInventoryChanged?.Invoke();
            Debug.Log($"[Inventory] Mision nueva: '{id}' ({category}). Total misiones: {Missions.Count}");
        }

        public void CompleteMission(string id)
        {
            foreach (var m in Missions)
            {
                if (m.id == id)
                {
                    m.completed = true;
                    OnInventoryChanged?.Invoke();
                    Debug.Log($"[Inventory] Mision completada: '{id}'.");
                    return;
                }
            }
        }

        public void RemoveMission(string id)
        {
            for (int i = 0; i < Missions.Count; i++)
            {
                if (Missions[i].id == id)
                {
                    Missions.RemoveAt(i);
                    OnInventoryChanged?.Invoke();
                    return;
                }
            }
        }

        public bool HasMission(string id)
        {
            foreach (var m in Missions) if (m.id == id) return true;
            return false;
        }

        public void Clear()
        {
            ReadNotes.Clear();
            CarriedItems.Clear();
            Missions.Clear();
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

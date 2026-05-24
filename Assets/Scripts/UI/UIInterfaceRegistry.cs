using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Catálogo de todas las interfaces UI del juego (autoría en escena vs runtime).
/// Referencia para correcciones visuales sin tocar lógica de gameplay.
/// </summary>
public static class UIInterfaceRegistry
{
    public enum UIBootstrap
    {
        SceneAuthored,
        SceneComponent,
        RuntimeBootstrap,
        RuntimeOnDemand
    }

    public readonly struct UIInterfaceEntry
    {
        public readonly string Id;
        public readonly string DisplayName;
        public readonly string ScriptType;
        public readonly UIBootstrap Bootstrap;
        public readonly string SceneOrContext;
        public readonly string Notes;

        public UIInterfaceEntry(string id, string displayName, string scriptType, UIBootstrap bootstrap, string sceneOrContext, string notes = "")
        {
            Id = id;
            DisplayName = displayName;
            ScriptType = scriptType;
            Bootstrap = bootstrap;
            SceneOrContext = sceneOrContext;
            Notes = notes;
        }
    }

    public static IReadOnlyList<UIInterfaceEntry> All { get; } = new List<UIInterfaceEntry>
    {
        // —— Menú principal (uGUI + TMP, escena) ——
        new("main_menu", "Menú principal", nameof(MainMenuController), UIBootstrap.SceneAuthored, "MainMenu",
            "Canvas_MainMenu: título, botones, opciones, confirmación, sliders de volumen."),
        new("main_menu_feedback", "Feedback botones menú", nameof(MainMenuButtonFeedback), UIBootstrap.SceneAuthored, "MainMenu",
            "Hover/select en botones del menú."),

        // —— Transiciones globales ——
        new("screen_fader", "Fundido pantalla", nameof(ScreenFader), UIBootstrap.RuntimeBootstrap, "Global (BeforeSceneLoad)",
            "Fade entre escenas."),
        new("scene_transition", "Transición escenas", nameof(SceneTransitionManager), UIBootstrap.RuntimeBootstrap, "Global",
            "Overlay de carga/fade coordinado con RunManager."),

        // —— HUD dungeon / hub (HUDCanvas en escena) ——
        new("hud_controller", "Orquestador HUD", nameof(HUDController), UIBootstrap.RuntimeBootstrap, "GameplayScene, CityArken, TestScene",
            "Crea/enlaza paneles si faltan; no en MainMenu."),
        new("health_panel", "Panel vida / rango", nameof(HealthPanelUI), UIBootstrap.RuntimeOnDemand, "HUD → HUDController",
            "Corazones, rango, barra de penalización."),
        new("resource_panel", "Panel recursos", nameof(ResourcePanelUI), UIBootstrap.RuntimeOnDemand, "HUD → HUDController",
            "Oro y recursos."),
        new("weapon_hud", "HUD armas", nameof(WeaponHudUI), UIBootstrap.SceneComponent, "HUDCanvas",
            "Arma equipada y munición."),
        new("mission_panel", "Panel misiones", nameof(MissionPanelUI), UIBootstrap.SceneComponent, "HUDCanvas",
            "Misiones diarias en run."),
        new("shadow_army_hud", "HUD ejército sombras", nameof(ShadowArmyHUD), UIBootstrap.SceneComponent, "HUDCanvas",
            "Iconos de sombras activas."),
        new("boss_hp", "Barra vida jefe", nameof(BossHPBar), UIBootstrap.SceneComponent, "HUDCanvas",
            "Visible en combate de jefe."),
        new("interaction_prompt", "Prompt interacción", nameof(InteractionPromptUI), UIBootstrap.SceneComponent, "HUDCanvas",
            "Texto tipo [E] interactuar."),
        new("minimap", "Minimapa", nameof(MinimapSystem), UIBootstrap.SceneComponent, "DungeonMinimap + HUDCanvas",
            "RawImage procedural del dungeon."),
        new("notifications", "Notificaciones", nameof(NotificationSystem), UIBootstrap.SceneComponent, "HUDCanvas",
            "Toasts y viñeta de dolor."),
        new("system_panel", "Panel System (level-up)", nameof(SystemPanelUI), UIBootstrap.SceneComponent, "HUDCanvas",
            "Asignación STR/AGI/RES al subir nivel."),
        new("system_unlock", "Desbloqueo System", nameof(SystemUnlockPanelUI), UIBootstrap.SceneComponent, "HUDCanvas",
            "Anuncio de desbloqueo del System."),

        // —— Sistema RPG ——
        new("system_manager_ui", "System (lógica UI)", nameof(SystemManager), UIBootstrap.SceneComponent, "DungeonSystems",
            "Abre SystemPanelUI en level-up."),

        // —— Hub CityArken ——
        new("hospital", "Hospital / Lira", nameof(HospitalUI), UIBootstrap.RuntimeOnDemand, "CityArken → CityHubBootstrapper",
            "Donaciones, historia Lira."),
        new("association", "Asociación cazadores", nameof(AssociationUI), UIBootstrap.RuntimeOnDemand, "CityArken → CityHubBootstrapper",
            "Misiones y empezar run."),
        new("meta_shop", "Mercado negro", nameof(MetaUpgradeShop), UIBootstrap.SceneComponent, "CityArken — prefab MetaUpgradeShop",
            "Mejoras permanentes. ESC cierra. Bloquea otras interfaces."),
        new("hub_prompt", "Prompt edificios hub", "Text (legacy)", UIBootstrap.RuntimeOnDemand, "CityHubBootstrapper",
            "[E] Entrar en edificios."),

        // —— Run flow ——
        new("pause_menu", "Menú pausa", nameof(PauseMenu), UIBootstrap.SceneComponent, "GameplayScene / TestScene — prefab PauseMenu",
            "ESC abre/cierra. Bloquea movimiento y otras UIs."),
        new("run_summary", "Resumen de run", nameof(RunSummaryUI), UIBootstrap.SceneComponent, "GameplayScene — prefab RunSummaryUI",
            "Fin de partida. ESC cierra tras animación."),

        // —— Jugador / mundo ——
        new("shadow_extraction", "Extracción sombra", nameof(ShadowExtractionSystem), UIBootstrap.RuntimeOnDemand, "Player prefab",
            "Barra de progreso hold-to-extract."),
        new("floating_text", "Texto flotante daño", nameof(FloatingTextPopup), UIBootstrap.RuntimeOnDemand, "Combat feedback",
            "TextMesh mundo, no uGUI."),
        new("hud_sprites", "Sprites HUD procedurales", nameof(HUDSpriteFactory), UIBootstrap.RuntimeOnDemand, "Varios paneles HUD",
            "Genera sprites pixel para corazones, cristal, sombra."),
    };
}

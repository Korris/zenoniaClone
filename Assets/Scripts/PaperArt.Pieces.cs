namespace ZenoniaProto
{
    /// <summary>
    /// Names of the kit pieces under Resources/Sprites/ui/paper/ (the only place the kit spells them). They are the names the art lane's
    /// cut tool writes (Tools/ui-paper-sheets/*.json); the "expected" ones are not cut yet: when they arrive under another name,
    /// change the name here, nothing else. A piece that is missing draws its flat fallback (PaperArt.Draw).
    /// </summary>
    public static class PaperPiece
    {
        // K1 frames
        public const string FramePaper = "kit/frame-paper-wood", FramePinned = "kit/frame-paper-pinned", FrameWood = "kit/frame-wood-board", FrameSlate = "kit/frame-slate-dark", FrameCard = "kit/card-paper-dashed";
        public const string TitleBoard = "kit/title-plaque-laurel", Ribbon = "kit/ribbon-red", Strip = "kit/section-strip-leather", Close = "kit/btn-close-red";
        // K2 buttons and tabs (a pressed button = its name + Pressed)
        public const string BtnGold = "kit/btn-gold", BtnBlue = "kit/btn-blue", BtnRed = "kit/btn-red", BtnPurple = "kit/btn-purple", BtnGrey = "kit/btn-grey-disabled", Pressed = "-pressed";
        public const string TabActive = "kit/tab-leather-active", TabInactive = "kit/tab-leather-inactive", PlankActive = "kit/menu-plank-active", PlankInactive = "kit/menu-plank-inactive";
        public const string ChipActive = "kit/chip-filter-active", ChipInactive = "kit/chip-filter-inactive";
        public const string ArrowLeft = "kit/btn-arrow-left", ArrowRight = "kit/btn-arrow-right", DotOn = "kit/page-dot-gold", DotOff = "kit/page-dot-dark", Plus = "kit/btn-plus-gold";
        // K3 slots and cards: prefix + PaperTheme.RarityArt(tier) (common, good, rare, heroic, legendary)
        public const string SlotPrefix = "kit/slot-item-", SlotEmpty = "kit/slot-item-empty", SlotLocked = "kit/slot-item-locked", CardPrefix = "kit/card-item-";
        public const string CardGlow = "kit/card-item-selected-frame", StarFull = "kit/star-full", StarEmpty = "kit/star-empty";
        public const string MarkAlertGold = "kit/badge-alert-gold", MarkCheck = "kit/badge-check-green", MarkLock = "kit/badge-lock-steel";
        // K4 badges, labels and bars
        public const string BadgeCount = "kit/badge-count-red", MarkAlert = "kit/badge-alert-red", MarkDot = "kit/badge-dot-red";
        public const string LabelRed = "kit/label-ribbon-red", LabelHot = "kit/label-ribbon-hot", LabelDark = "kit/label-ribbon-dark", LabelGreen = "kit/label-ribbon-green", LabelPurple = "kit/ribbon-label-purple";
        public const string BarLarge = "kit/bar-frame-large", BarSmall = "kit/bar-frame-small", BarFill = "kit/bar-fill-white";
        // H1 player status + toasts (phase 03: PlayerStatusBadge, HudBuffRow, RpgHud, NotificationToast); the crest pieces share one canvas height
        public const string Crest = "hud/crest-frame", CrestLowHp = "hud/crest-frame-low-hp", CrestPlate = "hud/crest-portrait-plate", CrestShield = "hud/crest-shield-overlay";
        public const string LevelTag = "hud/level-tag-leather", BuffSlot = "hud/buff-slot", RibbonLevelUp = "hud/ribbon-level-up", ToastStrip = "hud/toast-strip";
        public const string IconHp = "kit/icon-hp-heart", IconMp = "kit/icon-mp-drop";
        // B1 boss bars (phase 03: BossBarView, Monster.PaperOverheadBar)
        public const string BossBar = "boss/boss-bar-frame", BossMedallion = "boss/boss-medallion-gold", BossMedallionDefeated = "boss/boss-medallion-defeated", BossPlate = "boss/boss-medallion-portrait-plate";
        public const string CastBar = "boss/cast-bar-frame", CastMedallion = "boss/cast-medallion", CastPlate = "boss/cast-medallion-plate", RibbonWanted = "boss/ribbon-wanted";
        public const string RageGauge = "boss/rage-gauge-gold", RageMarker = "boss/rage-marker", EnragedBorder = "boss/enraged-fire-border", EnragedLabel = "boss/enraged-label", DefeatedPlaque = "boss/defeated-plaque-laurel";
        public const string OverheadNormal = "boss/overhead-bar-normal", OverheadElite = "boss/overhead-bar-elite", OverheadBoss = "boss/overhead-bar-boss";
        // HC compact HUD, status side (hudc/: PlayerStatusBadge, HudBuffRow, BossBarView, NotificationToast, WantedHudCard; on the HUD these replace the H1 / B1
        // pieces above, which only the bounty panel and old captures still use) and the LEVEL UP word (LevelUpWordmark).
        public const string HcPortraitRing = "hudc/status-portrait-ring", HcPortraitPlate = "hudc/status-portrait-plate", HcBarFrame = "hudc/status-bar-frame", HcBarFill = "hudc/status-bar-fill";
        public const string HcExpFrame = "hudc/status-exp-frame", HcExpFill = "hudc/status-exp-fill", HcLvPill = "hudc/status-lv-pill", HcBuffSlot = "hudc/buff-slot", HcToastPill = "hudc/toast-pill";
        public const string HcBossFrame = "hudc/boss-bar-frame", HcBossFill = "hudc/boss-bar-fill", HcBossRing = "hudc/boss-portrait-ring", HcCastFrame = "hudc/boss-cast-frame", HcCastFill = "hudc/boss-cast-fill";
        public const string HcWantedPill = "hudc/wanted-pill", HcWantedRing = "hudc/wanted-pill-portrait-ring", HcWantedAlive = "hudc/wanted-icon-alive", HcWantedRespawn = "hudc/wanted-icon-respawn", HcWantedClaimed = "hudc/wanted-icon-claimed";
        public const string LevelUpWordmark = "levelup/levelup-wordmark";
        // Chat notice board (phase 07: PaperChatPanelView, PaperChatFeedView). Only the dropdown is cut; the rest is expected from sheets C1 (chat/) and H2 (hud/, the round
        // wooden chat button): flat fallback until then. ChatRibbon is one white ribbon tinted per channel; ChatIconPrefix + all / world / map / party / whisper.
        public const string ChatDropdown = "kit/dropdown-paper", ChatButton = "hud/btn-chat-wood", ChatRibbon = "chat/ribbon-channel-white", ChatIconPrefix = "chat/icon-", ChatInput = "chat/input-paper", ChatQuill = "chat/title-quill";
        // A1 Armory Carousel (phase 05: ArmoryWindow). BagScene = the armory room without UI (PaperLayout background). Cut but not used yet:
        // bag/icon-filter-* and bag/icon-stat-* (chips and stat rows are text only), bag/resource-pill-dark (the kit's Chip below still has its expected name).
        public const string BagScene = "bag/armory-background", BagPlatform = "bag/dais-center", BagPedestal = "bag/pedestal", BagPedestalSelected = "bag/pedestal-selected";
        public const string BagPedestalLabel = "bag/pedestal-label", BagPedestalLabelSelected = "bag/pedestal-label-selected", BagLevelPlate = "bag/plate-level-red";
        public const string BagShelf = "bag/shelf-wood", BagPlaque = "bag/sign-bag-pouch", BagAnvil = "bag/sign-enhance-anvil", BagHint = "bag/tooltip-drag-hand", Dropdown = "kit/dropdown-paper";
        // H2 entry buttons under the minimap (phase 04: HudEntryButtons). A pressed tag / blade = its name + Pressed; EntryIconPrefix + bag / skill / pet / gacha / quests / settings.
        public const string EntryBoard = "hud/entry-hanging-board", EntryTag = "hud/entry-tag", MoreClosed = "hud/more-pouch-closed", MoreOpen = "hud/more-pouch-open", FanBlade = "hud/more-fan-blade";
        public const string EntryIconPrefix = "hud/icon-entry-", Keycap = "hud/keycap-dark", KeycapWide = "hud/keycap-dark-wide";
        // H3 touch controls and PC hotbar (phase 04: PaperTouchPad, PaperSkillSlot, PaperSkillArc, PaperHotbar). The slot / potion plates are the white windows of their rings (icon mask, cooldown sweep).
        public const string JoystickBase = "hud/joystick-base", JoystickKnob = "hud/joystick-knob", Attack = "hud/btn-attack", SkillRing = "hud/skill-slot-ring", SkillPlate = "hud/skill-slot-plate";
        public const string SkillEmpty = "hud/skill-slot-empty", SkillLocked = "hud/skill-slot-locked", PotionRing = "hud/potion-slot-ring", PotionPlate = "hud/potion-slot-plate", CostPill = "hud/cost-pill-dark";
        public const string SkillBelt = "hud/skill-arc-belt", HotbarBelt = "hud/hotbar-belt-leather", PagePlaque = "hud/page-switch-plaque";
        // S1 Training Hall (phase 06: TrainingHallWindow, PaperSkillStatue, PaperSkillBeltSlot). HallScene = the hall without UI (PaperLayout background).
        // The three statues share one 256 x 320 canvas; HallStatuePlate is the white window of their ring (icon mask). HallRingPrefix + learned / unlearned / locked / purple.
        public const string HallScene = "skill/training-hall-background", HallShelf = "skill/shelf-two-tier", HallScroll = "skill/detail-scroll", HallSign = "skill/condition-sign-arrows";
        public const string HallStatueGold = "skill/statue-gold", HallStatueSilver = "skill/statue-silver", HallStatueStone = "skill/statue-stone", HallStatuePlate = "skill/statue-icon-plate";
        public const string HallBrackets = "skill/select-brackets-gold", HallRankPill = "skill/rank-pill-dark", HallMaxRibbon = "skill/ribbon-max-purple", HallLaurel = "skill/laurel-pair-gold", HallRingPrefix = "kit/skill-ring-";
        public const string HallBook = "skill/magazine-book", HallBelt = "skill/magazine-belt", HallSlotEmpty = "skill/magazine-slot-empty", HallSlotFrame = "skill/magazine-slot-frame", HallSlotPlate = "skill/magazine-slot-plate";
        public const string HallSlotAssign = "skill/magazine-slot-assign", HallSlotTab = "skill/magazine-number-tab", HallHint = "skill/assign-hint-banner", HallArrow = "skill/arrow-down-yellow";
        public const string HallIconSp = "skill/icon-sp-orb", HallIconCooldown = "skill/icon-cooldown-hourglass", HallIconRankUp = "skill/icon-rank-up", HallIconAssign = "skill/icon-assign-swap", HallIconRespec = "skill/icon-respec";
        public const string HallIconWarning = "skill/icon-warning", HallIconBasic = "skill/icon-tab-basic", HallIconAdvanced = "skill/icon-tab-advanced", IconGold = "kit/icon-gold-coin";
        // W1 Wanted Boss (phase 11: WantedBoardWindow, WantedHudCard, WantedNotices, WantedRewardPanel, WantedGuide, WantedMinimapMark). WantedScene = the town square with
        // the empty board (PaperLayout background). A "...Plate" is the white window of the ring on the same canvas as its banner / card (portrait mask).
        // WantedDayTagPrefix + grey / red / purple (yesterday, today, tomorrow); WantedDodgePrefix + line / fan / multi-circle / safe-lane.
        public const string WantedScene = "wanted/wanted-square-background", WantedPoster = "wanted/poster-paper", WantedPortraitFrame = "wanted/poster-portrait-frame", WantedPortraitPlate = "wanted/poster-portrait-plate";
        public const string WantedRook = "wanted/guard-rook", WantedRibbonDaily = "wanted/ribbon-daily-stars", WantedPlaqueDark = "wanted/status-plaque-dark", WantedPlaqueSteel = "wanted/status-plaque-steel";
        public const string WantedRewardSlot = "wanted/reward-slot-dark", WantedStampOpen = "wanted/stamp-not-claimed-yet", WantedStampClaimed = "wanted/stamp-claimed", WantedTimerBar = "wanted/timer-bar-clock";
        public const string WantedSectionTag = "wanted/section-tag-cream", WantedScheduleCard = "wanted/schedule-card", WantedScheduleToday = "wanted/schedule-card-selected-frame", WantedDayTagPrefix = "wanted/day-tag-";
        public const string WantedSkillRing = "wanted/boss-skill-ring-blue", WantedSkillPlate = "wanted/boss-skill-plate", WantedDodgePrefix = "wanted/dodge-";
        public const string WantedHudCard = "wanted/hud-card-hanging", WantedHudCardPlate = "wanted/hud-card-portrait-plate", WantedBannerLogin = "wanted/banner-login-laurel", WantedBannerLoginPlate = "wanted/banner-login-portrait-plate";
        public const string WantedBannerRespawn = "wanted/banner-respawn-blue", WantedBannerRespawnPlate = "wanted/banner-respawn-portrait-plate", WantedBannerArea = "wanted/banner-area-map-arrow";
        public const string WantedRibbonWarning = "wanted/ribbon-warning-chains", WantedRibbonBounty = "wanted/ribbon-bounty-laurel", WantedStripAlready = "wanted/strip-already-claimed";
        public const string WantedSkullAlive = "wanted/minimap-skull-alive", WantedSkullRespawning = "wanted/minimap-skull-respawning";
        public const string WantedIconAlive = "wanted/icon-alive-skull", WantedIconRespawn = "wanted/icon-respawn", WantedIconClock = "wanted/icon-clock", WantedIconPin = "wanted/icon-map-pin", WantedIconChevron = "wanted/icon-chevron-right";
        // P1 / P1b Pet Camp (phase 08: PetCampWindow, PetCampNest, PetCampCard). PetScene = the camp without UI (PaperLayout background). A lit nest / a selected
        // card = its name + PetSelected. PetRoleIconPrefix + combat / support / stat; PetRarityPrefix + n / r / sr / ssr / ur (the badges carry their letters).
        public const string PetScene = "pet/pet-camp-background", PetNestLarge = "pet/nest-large", PetNestSmall = "pet/nest-small", PetSelected = "-selected", PetCard = "pet/pet-card-slot", PetCardUnknown = "pet/pet-card-unknown";
        public const string PetRoleSign = "pet/role-sign-ribbon", PetRoleIconPrefix = "pet/icon-role-", PetPercentPill = "pet/percent-pill-red", PetBonusPlaque = "pet/team-bonus-plaque", PetBonusPlaqueGrey = "pet/team-bonus-plaque-grey";
        public const string PetBondCard = "pet/bond-card", PetBondCardActive = "pet/bond-card-active", PetTagActive = "pet/tag-active-green", PetTagPassive = "pet/tag-passive-purple", PetTagSkill = "pet/tag-skill-blue";
        public const string PetShelfSign = "pet/sign-owned-pets-hanging", PetGrowthCard = "pet/growth-card-gold", PetRarityPrefix = "pet/rarity-", PetPlankGacha = "pet/menu-plank-gacha", PetPlankGachaActive = "pet/menu-plank-gacha-active";
        public const string PetCollectionHeader = "pet/collection-header-book", PetStationSign = "pet/station-sign-wood", PetFoodTrough = "pet/station-food-trough", PetCrystalAltar = "pet/station-crystal-altar";
        public const string PetBall = "pet/ball-pet", PetBallRare = "pet/ball-rare", IconCaptureBall = "kit/icon-capture-ball", IconCaptureBallRare = "hud/icon-capture-ball-rare";
        public const string IconGachaCoin = "kit/icon-gacha-coin", IconPetCrystal = "kit/icon-pet-crystal", IconPetFood = "kit/icon-pet-food", IconShard = "kit/icon-shard";
        // G1 Lumi's counter (phase 09: LumiCounterWindow, PaperGachaButton, PaperGachaCard). GachaScene = the stall without UI (PaperLayout background). A pressed
        // Buy / Pull button = its name + Pressed. Rarity labels, coin / crystal / shard icons: the Pet Camp names above (PetRarityPrefix, IconGachaCoin ...).
        public const string GachaScene = "gacha/gacha-stall-background", GachaLumi = "gacha/lumi-bust", GachaBubble = "gacha/speech-bubble", GachaDais = "gacha/egg-dais", GachaEgg = "gacha/egg-ice-large";
        public const string GachaNestPurple = "gacha/egg-nest-purple", GachaNestGold = "gacha/egg-nest-gold", GachaEggshell = "gacha/eggshell-broken", GachaStarburst = "gacha/hatch-starburst";
        public const string GachaBanner = "gacha/banner-featured-purple", GachaRibbonFeatured = "gacha/ribbon-featured-red", GachaRibbonNew = "gacha/ribbon-new-green", GachaHelp = "gacha/btn-help-round";
        public const string GachaBtnBuy = "gacha/btn-buy-gold", GachaBtnPullBlue = "gacha/btn-pull-blue", GachaBtnPullPurple = "gacha/btn-pull-purple", GachaPityBar = "gacha/pity-bar-frame";
        public const string GachaPlaque = "gacha/shard-exchange-plaque", GachaPanel = "gacha/panel-paper-wood", GachaTitleTag = "gacha/panel-title-tag", GachaFlagGold = "gacha/flag-tab-gold", GachaFlagWood = "gacha/flag-tab-wood";
        public const string GachaResultCard = "gacha/result-card-white-rim";
        // Gacha pieces of the result mockup that were not cut (flat fallback): the dark laurel plate (pet name, "Joined your collection", x10 summary) and the slate Cancel / Skip button.
        public const string GachaResultPlate = "gacha/result-plate-laurel", GachaBtnSlate = "gacha/btn-slate";
        // HC compact HUD controls (hudc/: HudEntryButtons, PaperTouchPad, PaperSkillArc, PaperSkillSlot, PaperPotionButton, PaperHotbar, PaperChatFeedView,
        // PaperChatBarView). A pressed plate / button / ring = its name + Pressed; CMenuIconPrefix + bag / skill / pet / quests / more / gacha / settings / chat.
        public const string CMenuPlate = "hudc/menu-icon-plate", CMenuIconPrefix = "hudc/menu-icon-", CBadgeDot = "hudc/badge-dot-red", CBadgeCount = "hudc/badge-count-red", CBadgeAlert = "hudc/badge-alert";
        public const string CJoyRing = "hudc/joy-ring", CJoyKnob = "hudc/joy-knob", CAttack = "hudc/attack-btn", CDash = "hudc/dash-btn", CPageDotOn = "hudc/page-dot-on", CPageDotOff = "hudc/page-dot-off";
        public const string CSkillRing = "hudc/skill-slot-ring", CSkillEmpty = "hudc/skill-slot-empty", CSkillLocked = "hudc/skill-slot-locked", CSkillDim = "hudc/skill-slot-dim", CCostPill = "hudc/skill-cost-pill";
        public const string CPotionRing = "hudc/potion-ring", CPotionRingEmpty = "hudc/potion-ring-empty", CHotbarSlot = "hudc/hotbar-slot", CKeycap = "hudc/hotbar-keycap", CKeycapWide = "hudc/hotbar-keycap-wide";
        public const string CExpFrame = "hudc/hotbar-exp-frame", CExpFill = "hudc/hotbar-exp-fill", CChatBar = "hudc/chat-input-bar", CChatPill = "hudc/chat-channel-pill", CChatSend = "hudc/chat-send-btn";
        // Expected: asked for in the plan but not cut on 2026-11-16 (the kit draws their flat fallback)
        public const string SlotGlow = "kit/slot-item-selected-frame", Chip = "kit/chip-resource";
    }
}

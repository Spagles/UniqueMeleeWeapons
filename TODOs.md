# TODOs

## Cleanup

1. Existing traits/features
   - Mod settings to add each weapon to rewards pools? what pools are appropriate/similar to what are the tradeoffs?
   - Mod setting to remove wood stuff from pools for generated weapon rewards, enabled by default
   - Mod setting slider for the warband quest commonality, showing our default, as well as the vanilla ancient mercenaries commonality for reference
   - Review blood-stained on-hit flee chance (is it a bit low?)
   - blood-stained moodlet shows dupe on bloodlust pawn
   - Some weapons not showing expected traits as valid items in our companion mod's (../UniqueWeaponsUnbound/) customization dialog's rhs trait selector e.g. mace
     - ROOT CAUSE (traced 2026-07-21, fix belongs in UWU): not a category mis-link — a full UMW def audit found all weapon/trait/category links correct, and UWU's candidate list (TraitValidationUtility.GetCompatibleTraits) includes them. But Dialog_WeaponCustomization defaults hideNegativeTraits=true, and TraitCostUtility.IsNegativeTrait counts any MarketValue statFactor <1 — so UMW_BloodStained/UMW_Ugly/UMW_Cumbersome (x0.8) are hidden until "show negative traits" is ticked. Candidate UWU fixes: grey-with-reason instead of hide, and/or don't class effect-carrying traits (blood-stained) as purely negative.
   - Can we add a line displaying the stuff on UMW inspect pane
   - Can we add stuffable adjectives to grammar generator for UMW names?

2. Complete trait roster
   - Evaluate current trait roster count and spread against Odyssey's (e.g. Docs/Odyssey-Ranged-Trait-Catalog.md)
   - Brainstorm ideas for possible Ability traits (heirloom trait with inspiring shout? aoe shock stun? aoe cleave attack?) these are hard because melee weapons are less thematically configurable than Odyssey's ranged, i.e. can't put a grenade launcher on a sword.
   - Brainstorm ideas for more color-two traits

3. Consider Mod integration
   - Investigate possible VWE integration. Installed locally at "C:\Program Files (x86)\Steam\steamapps\workshop\content\294100\1814383360". Can we reuse any of their thematically generic traits without reimplementing their functional code? akimbo etc would be nice.

4. Update readme and About.xml

## Art

- ModIcon.png

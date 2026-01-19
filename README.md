# Custom Republic

## Features
- Create a republic at game start
    - select republic member factions
    - set senator pawnkind
- Invite faction to your republic if your are allies
- 11 new Perks

## How does it work
During the world creations screen where you select the factions for your world will be a new button to customize the republic. 
There you can select the initial members of your republic. You can also select the pawnkind of the senators. If default is choosen it will use the senator pawnkind from VFE Classical (the ones with the togas).
During game start perks and research will be distributed randomly to republic senators. Each faction can only have research associated to it senators that is of the same tech level than the faction. With default settings first tech printed ressearch is distributed first. If there is no more tech printed research avalable normal research will be used instead. There is also an option to disable tech printed research for distribution if you want to get it the old fashioned way. If you dont care about the research aspect you can also enable the option to assign dummy research. In that case every senators will be assigned the same a low tech research usually tree sowing. The Perks will be distributed randomly. If there are more senators than perks perks might be assigned twice.

If you want more control you use xml patches to add FactionExtension_SenatorInfoExtended via the [PatchOperationAddModExtension](https://rimworldwiki.com/wiki/Modding_Tutorials/PatchOperations#PatchOperationAddModExtension) operation. FactionExtension_SenatorInfoExtended is essentially the them as the FactionExtension_SenatorInfo from VFE Classical with additionally the pawnkind field.
Whole XML structure of the mod extension:
```xml
<li Class="CustomRepublic.FactionExtension_SenatorInfo">
    <numSenators>5</numSenators>
    <senatorPerks>
        <li>AmorVincitOmnia</li>
        <li>CarpeDiem</li>
        <li>NilDesperandum</li>
        <li>PanemEtCircenses</li>
        <li>ArsLongaVitaBrevis</li>
    </senatorPerks>
    <senatorResearch>
        <li>VFEC_Togas</li>
        <li>VFEC_TemperatureControl</li>
        <li>VFEC_MeatDrying</li>
        <li>VFEC_Mosaics</li>
        <li>VFEC_Thermaebath</li>
    </senatorResearch>
    <finalResearch>VFEC_DramaAndComedy</finalResearch>
    <finalPerk>Tributum</finalPerk>
    <perkBGPath>UI/Perks/PerkBG_CentralRepublic</perkBGPath>
    <pawnkind></pawnkind> <!-- leaving this blank is the same as chosing default in the 
                               republic customisation screen -->
</li>"
```

During a game you can invite allies to join the republic. For this either travel to the faction and use the gizmo at the bottom or call them via the comms console. Perks and research will be assigned similiar as if they were enabled from start. The new faction will respect the choosen senator pawnkind in the customize republic menu. This cannot be changed after a game started except for savegame editing.

In order to change pawnkind, research or perks after game start the save game has to be editied. Just search for "CustomRepublic.GameComponent_Republic". Make backups and do this at your own risk. I will not promise support/fixing for errors/bugs that happen because of save game editing.

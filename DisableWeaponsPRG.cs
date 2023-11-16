using CounterStrikeSharp.API.Core;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DisableWeaponsPRG;
public class DisableWeaponsPRG : BasePlugin
{
    public override string ModuleAuthor => "sphaxa";
    public override string ModuleName => "DisableWeapons for PRG";
    public override string ModuleVersion => "v1.0.1";

    private Config _config = new(false, new List<string>{"weapon_awp"});

    public override void Load(bool hotReload)
    {
        var configPath = Path.Join(ModuleDirectory, "DisableWeaponsConfig.json");
        _config = !File.Exists(configPath) ? CreateConfig(configPath) : JsonSerializer.Deserialize<Config>(File.ReadAllText(configPath));

        RegisterEventHandler<EventItemPickup>(OnItemPickup, HookMode.Post);
        RegisterEventHandler<EventItemPurchase>(OnItemPurchase, HookMode.Post);

        RegisterListener<Listeners.OnEntitySpawned>(entity =>
        {
            if (entity == null) return;
            if (entity.Entity == null) return;
            if (!entity.DesignerName.StartsWith("weapon_")) return;

            if (!IsBlocked(entity.DesignerName)) return;

            entity.Remove();
        });
    }

    private HookResult OnItemPickup(EventItemPickup @event, GameEventInfo info)
    {
        if (!@event.Userid.IsValid)
        {
            return HookResult.Continue;
        }

        CCSPlayerController player = @event.Userid;

        if (player.Connected != PlayerConnectedState.PlayerConnected)
        {
            return HookResult.Continue;
        }

        if (!player.PlayerPawn.IsValid)
        {
            return HookResult.Continue;
        }
        var wep = MetaData.Weapons.FirstOrDefault(w => w.DefIndex == @event.Defindex);
        if (wep == null) return HookResult.Continue;

        if (!IsBlocked(wep.WeaponName)) return HookResult.Continue;

        if (player.PlayerPawn.Value.WeaponServices == null) return HookResult.Continue;
        foreach (var invwep in player.PlayerPawn.Value.WeaponServices.MyWeapons)
        {
            if (invwep.Value.AttributeManager.Item.ItemDefinitionIndex != @event.Defindex) continue;
            invwep.Value.Remove();
            NativeAPI.IssueClientCommand((int)@event.Userid.EntityIndex!.Value.Value - 1, "lastinv");
        }

        return HookResult.Continue;
    }

    private HookResult OnItemPurchase(EventItemPurchase @event, GameEventInfo info)
    {
        if (!@event.Userid.IsValid)
        {
            return HookResult.Continue;
        }

        CCSPlayerController player = @event.Userid;

        if (player.Connected != PlayerConnectedState.PlayerConnected)
        {
            return HookResult.Continue;
        }

        if (!player.PlayerPawn.IsValid)
        {
            return HookResult.Continue;
        }


        var wep = MetaData.Weapons.FirstOrDefault(w => w.WeaponName == @event.Weapon);
        if (wep == null) return HookResult.Continue;

        if (!IsBlocked(wep.WeaponName)) return HookResult.Continue;

        @event.Userid.InGameMoneyServices!.Account += wep.Price;

        return HookResult.Continue;
    }

    private static Config CreateConfig(string configPath)
    {
        var data = new Config(false, new List<string> { "weapon_awp" });
        File.WriteAllText(configPath, JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping }));
        return data;
    }

    private bool IsBlocked(string DesignerName)
    {
        bool exists = _config.Names.Contains(DesignerName);
        return _config.Whitelist ? !exists : exists;
    }
}

public class WeaponMeta
{
    public string? WeaponName { get; init; }
    public string? Name { get; init; }
    public int Price { get; init; }
    public long DefIndex { get; init; }
}

public class Config
{
    [JsonPropertyName("Whitelist")]
    public required bool Whitelist { get; set; }

    [JsonPropertyName("Names")]
    public List<string>? Names { get; set; }

    [JsonConstructor]
    [SetsRequiredMembers]
    public Config(bool whitelist, List<string> names)
    {
        Whitelist = whitelist;
        Names = names;
    }
}
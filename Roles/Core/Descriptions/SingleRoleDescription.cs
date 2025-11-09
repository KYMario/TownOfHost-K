namespace TownOfHost.Roles.Core.Descriptions;

/// <summary>
/// Mod役職の説明文
/// </summary>
public class SingleRoleDescription : RoleDescription
{
    public SingleRoleDescription(SimpleRoleInfo roleInfo) : base(roleInfo)
    {
        BlurbKey = $"{roleInfo.RoleName}{BlurbSuffix}";
        DescriptionKey = $"{RoleInfo.RoleName}{DescriptionSuffix}";
    }

    /// <summary>短いひとこと説明文の翻訳キー</summary>
    public string BlurbKey { get; }
    /// <summary>長い説明文の翻訳キー</summary>
    public string DescriptionKey { get; }
    public override string Blurb => Translator.GetString(BlurbKey);
    public override string Description
    {
        get
        {
            if (RoleInfo.Desc == null) return Translator.GetString(DescriptionKey);
            var CustomDescription = RoleInfo.Desc();

            if (CustomDescription == null) return Translator.GetString(DescriptionKey);
            return CustomDescription;
        }
    }
    public const string BlurbSuffix = "Info";
    public const string DescriptionSuffix = "InfoLong";
}

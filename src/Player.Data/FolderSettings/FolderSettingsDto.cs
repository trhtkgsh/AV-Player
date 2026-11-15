namespace Player.Data.FolderSettingsStorage;

// One entry of the folder_settings row. Written as {"Speed":1.5,"Favorite":true}; absent members are omitted.
internal sealed class FolderSettingsDto
{
	public float? Speed { get; set; }
	public bool? Favorite { get; set; }
}

namespace Core.Render
{
    /// <summary>
    /// Fixed-camera zone for Social areas (Resident Evil–style tank cameras). Pure camera
    /// switching, no room content to activate — unlike OverworldRoomRender, everything a Social
    /// area needs is already in the scene at once.
    ///
    /// Setup per area:
    ///   1. Create an empty GameObject with a collider set as Trigger.
    ///   2. Add this component and assign the CinemachineCamera for this area.
    ///   3. The CinemachineBrain on the main camera handles the blend.
    /// </summary>
    public class SocialAreaRender : CameraZone
    {
    }
}
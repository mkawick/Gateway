/// <summary>
/// An entity which exists in the world at a given position and rotation
/// </summary>
using Vectors;
public class ClientWorldEntity : ClientEntity
{
    // TODO: Move this into a transform state component?
    public Vector3 Position { get; set; }
    public Vector3 Rotation { get; set; }
}

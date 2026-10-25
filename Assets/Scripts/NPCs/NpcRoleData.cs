using UnityEngine;

[CreateAssetMenu(menuName = "Village/NPC Role", fileName = "NewNpcRole")]
public sealed class NpcRoleData : ScriptableObject
{
    public string displayName = "Trabalhador";
    [TextArea] public string description;
    public VillageFacilityKind workplaceKind = VillageFacilityKind.Production;
    [Tooltip("Opcional: restringe a função a um tipo específico de instalação.")]
    public VillageFacilityData requiredFacility;
    [Min(1)] public int slotsPerFacility = 1;
    public bool providesService;
    public bool productionWorker;

    public bool Accepts(VillageFacility facility) => facility != null && facility.IsOperational &&
        facility.data.kind == workplaceKind && (requiredFacility == null || facility.data == requiredFacility);
}

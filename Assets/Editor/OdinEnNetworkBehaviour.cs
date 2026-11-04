// =============================================================================
// PARA QUE ...... Netcode dibuja los NetworkBehaviour con su propio Inspector y ahi
//                 los [Button] de Odin no salen. Con esto los dibuja Odin.
// VA EN ......... una carpeta llamada Editor (Assets/Editor). Se copia una sola vez.
// =============================================================================
using Sirenix.OdinInspector.Editor;
using Unity.Netcode;
using UnityEditor;

[CustomEditor(typeof(NetworkBehaviour), true)]
public class OdinEnNetworkBehaviour : OdinEditor { }

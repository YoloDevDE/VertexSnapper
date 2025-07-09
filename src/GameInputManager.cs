using UnityEngine;
using VertexSnapper.Input;

namespace VertexSnapper;

public class GameInputManager : MonoBehaviour
{
    private void Update()
    {
        KeyInput.Update();
    }
}
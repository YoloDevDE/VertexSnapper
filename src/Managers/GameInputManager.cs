using UnityEngine;
using VertexSnapper.Input;

namespace VertexSnapper.Managers;

public class GameInputManager : MonoBehaviour
{
    private void Update()
    {
        KeyInput.Update();
    }
}
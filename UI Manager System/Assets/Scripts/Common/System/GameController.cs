using Generic;
using Generic.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameController : PrefabSingleton<GameController>
{
    [SerializeField]
    private WindowManager _windowManagerPrefab;
    public WindowManager WindowManager { get; private set; }

    protected override void Init()
    {
        base.Init();

        WindowManager = Instantiate(_windowManagerPrefab, null, false);
        WindowManager.Init();
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

public class PlayerController : MonoBehaviour
{
    public void Init(PlayerInfo info)
    {
        var timeline = gameObject.AddComponent<PlayerTimelineController>();
        timeline.Init(info.AbilityList);

        if (info.PlayerType == PlayerType.Hero)
        {
            gameObject.AddComponent<PlayerMoveController>();
        }
    }
}

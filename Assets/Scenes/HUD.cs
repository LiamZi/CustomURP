using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class HUD : MonoBehaviour
{
    public float _updateInterval = 0.5f;
    private float _accum = 0;
    private float _frames = 0;
    private float _timeLeft;
    private Color _color = Color.white;
    private float _fps = 0f;
    private GUIStyle _guiStyle = new GUIStyle();
    
    // Start is called before the first frame update
    void Start()
    {
        _timeLeft = _updateInterval;
    }

    // Update is called once per frame
    void Update()
    {
        _timeLeft -= Time.deltaTime;
        _accum += Time.timeScale / Time.deltaTime;
        ++_frames;

        if (_timeLeft <= 0.0)
        {
            _fps = _accum / _frames;

            if (_fps < 30)
            {
                _color = Color.yellow;
            }
            else
            {
                if (_fps < 10)
                {
                    _color = Color.red;
                }
                else
                {
                    _color = Color.green;
                }
            }

            _timeLeft = _updateInterval;
            _accum = 0.0f;
            _frames = 0;
        }
    }

    private void OnGUI()
    {
        _guiStyle.fontSize = 25;
        GUI.skin.label.fontSize = 25;
        GUI.color = _color;
        GUI.Label(new Rect(Screen.width - 150, 10, 400, 40), $"{_fps:F2} FPS");
    }
}

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

public class ToolMain : MonoBehaviour
{
    public Image LeftImage;

    public Image RightImage;

    public GameObject Tip;

    public Transform TipHolder;
    // Start is called before the first frame update
    void Start()
    {
        RightImage.raycastTarget = true;                                                                                                    
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            var point = Input.mousePosition;
            var tip = Instantiate(Tip);
            tip.transform.SetParent(TipHolder);
            tip.transform.localPosition = point;
            tip.transform.localScale = Vector3.one;
        }

        //OnDrawGizmos();
    }
    
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class Viewport : MonoBehaviour
{
    [SerializeField]
    [Range(4, 16)]
    float size = 4;
    float angle;

    [SerializeField]
    CellsGenerator cellsGenerator;

    [SerializeField]
    InputField seamField;
    [SerializeField]
    InputField angleField;
    [SerializeField]
    InputField offsetField;
    [SerializeField]
    Text areaText;

    // Start is called before the first frame update
    void Start()
    {
        /*SetViewPortSize();
        ShowCells();*/
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnEnable()
    {
        seamField.onValueChanged.AddListener( ValidationSeamField );
        offsetField.onValueChanged.AddListener( ValidationOffsetField );
        seamField.onSubmit.AddListener( SubmitSeamSize );
        offsetField.onSubmit.AddListener( SubmitOffset );
        angleField.onSubmit.AddListener( SubmitAngle );
        cellsGenerator.OnAreaCalculated += ShowWallArea;

        InitVals();
        InitNewWall();
    }

    private void OnDisable()
    {
        seamField.onValueChanged.RemoveListener( ValidationSeamField );
        offsetField.onValueChanged.RemoveListener( ValidationOffsetField );
        seamField.onSubmit.RemoveListener( SubmitSeamSize );
        offsetField.onSubmit.RemoveListener( SubmitOffset );
        angleField.onSubmit.RemoveListener( SubmitAngle );
        cellsGenerator.OnAreaCalculated -= ShowWallArea;

    }


    void InitVals()
    {
        //Math.Abs на случай,если в инспекторе в поле текст прописать значение с минусом
        cellsGenerator.SeamSize = Mathf.Abs( int.Parse( seamField.text ) );
        cellsGenerator.OffsetX = Mathf.Abs( int.Parse( offsetField.text ) );
        angle = int.Parse( angleField.text );
    }

    void SubmitSeamSize( string text )
    {
        Debug.Log( "Submit" );
        int val = 0;
        if ( int.TryParse( text, out val ) )
        {
            cellsGenerator.SeamSize = val;
            InitNewWall();
        }
    }

    void SubmitOffset( string text )
    {
        int val = 0;
        if ( int.TryParse( text, out val ) )
        {
            cellsGenerator.OffsetX = val;
            InitNewWall();
        }
    }

    void SubmitAngle( string text )
    {
        int val = 0;
        if ( int.TryParse( text, out val ) )
        {
            angle = val;
            InitNewWall();
        }
    }

    void ValidationSeamField(string txt )
    {
        Debug.Log( "Validation" );
        if ( txt.Length > 0 && txt[0] == '-' ) seamField.text = txt.Remove( 0, 1 );
    }

    void ValidationOffsetField( string txt )
    {
        if ( txt.Length > 0 && txt[0] == '-' ) offsetField.text = txt.Remove( 0, 1 );
    }

    void ShowWallArea( float area )
    {
        areaText.text = string.Format( "{0:0.00} м2", area );
    }    

    void InitNewWall()
    {
        SetViewPortSize();
        ShowCells();
    }


    void SetViewPortSize()
    {
        this.transform.localScale = new Vector3( size, size, 0.1f );
    }


    void ShowCells()
    {
        cellsGenerator.MakeWall( size, angle );
    }

}

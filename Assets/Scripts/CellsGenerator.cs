using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class CellsGenerator : MonoBehaviour
{
    Vector2 rectSize;      //������� ������������ �������
    [SerializeField]
    GameObject cellPrefab; //��������� ������
    [SerializeField]
    GameObject cellPlane;  //������������ ������� ������, ����� ������� ������������ �������

    [SerializeField]
    Vector2 cellSize;     //������ ������

    float seamSize_mm = 100;  //������ ��� ����� �������� � �����������
    float offsetX_mm = 10;    //�������� ����� � �����������

    public float OffsetX { get { return offsetX_mm * 0.001f;} set { offsetX_mm = value; } }
    public float SeamSize { get { return seamSize_mm * 0.001f; } set { seamSize_mm = value; }  }

    List<Ray> viewPortRaysList = new List<Ray>();
    List<Cell> allCellsInScene = new List<Cell>();
    List<RaycastHit> hits = new List<RaycastHit>();

    ///������� ����������� ��� ��������� ������� �������
    public event UnityAction<float> OnAreaCalculated;

    //������� �����
    float wallArea;

    bool shouldCalcArea;
    float time = 0.2f;
    float timer = 0.0f;
    void Update()
    {
        //����������� ������� � Update ����� ��������� ��������
        //TODO: ����� ������� � ��������
        if ( shouldCalcArea )
        {
            if( timer >= time )
            {
                CalculateArea();
                shouldCalcArea = false;
                timer = 0.0f;
            }
            else
            {
                timer += Time.deltaTime;
            }
        }
    }

    //������� ������� �������
    public void CalculateArea()
    {
        wallArea = 0.0f;
        shouldCalcArea = false;
        allCellsInScene.ForEach( cell => cell.Clear() ); ;

        hits.Clear();
        for( int i = 0; i < viewPortRaysList.Count;i++ )
        {
            RaycastHit[] raycastHits = Physics.RaycastAll( viewPortRaysList[i], 100 );
            if(raycastHits.Length != 0 )
            {
                hits.AddRange( raycastHits );
            }
        }


        hits.ForEach( x =>
        {
            Cell cell = x.collider.GetComponent<Cell>();
            if ( cell != null )
                cell.AddHitPoint( x.point );
        } );

        Rect rect = new Rect();

        rect.xMin = -rectSize.x;
        rect.xMax = rectSize.x;
        rect.yMin = -rectSize.y;
        rect.yMax = rectSize.y;

        allCellsInScene.ForEach( cell => wallArea += cell.CalculateAreaInViewPort( ref rect ) );
        OnAreaCalculated?.Invoke( wallArea );
    }

    public void MakeWall( float sizeOfViewPort, float angle )
    {
        ClearCells();
        SetPlaneRotation( 0 );
        rectSize = new Vector2( sizeOfViewPort/2, sizeOfViewPort/2 );
        CreateViewportRays( sizeOfViewPort );
        GenerateCells( sizeOfViewPort );
        SetPlaneRotation( angle );
        shouldCalcArea = true;
    }

    void SetPlaneRotation( float angle )
    {
        cellPlane.transform.rotation = Quaternion.Euler( 0.0f, 0.0f, angle );
    }

    //������� ����� ��������� ������
    void ClearCells()
    {
        allCellsInScene.ForEach( cell => Destroy( cell.gameObject ) );
        allCellsInScene.Clear();
    }

    //������� ���������� row ��� �� countCols ������ �� �������� sizeOfViewPort 
    void InstantiateRow( int countCols, int row, float sizeOfViewPort )
    {
        Vector3 startPoint = Vector3.zero;
        //��������� ����� � ������
        for ( int col = 0; col < countCols; col++ )
        {
            //�������� ���� ������������ ����� �������, � �������� �� - cellsSize.x �� 0
            float offsetXN = ( OffsetX * Mathf.Abs(row) - cellSize.x ) - SeamSize;
            if ( offsetXN > 0 )
            {
                offsetXN %= cellSize.x;
                offsetXN -= cellSize.x;
            }

            //����� ����������������� ����� ������
            Vector3 point = new Vector3( ( -sizeOfViewPort + cellSize.x ) / 2 + offsetXN + ( cellSize.x + SeamSize ) * col,
                ( -sizeOfViewPort + cellSize.y ) / 2 + row * ( cellSize.y + SeamSize ) - SeamSize, 0.0f );
            if ( col == 0 )
                startPoint = point;

            GameObject gameObject = Instantiate( cellPrefab, point, Quaternion.identity, cellPlane.transform );
            gameObject.transform.localScale = new Vector3( cellSize.x, cellSize.y, 0.01f );

            allCellsInScene.Add( gameObject.GetComponent<Cell>() );


            //��������� ������ ����� � ������ �� ��������� ����, ��� ����������� �������� ������
            if ( col <= countCols / 2   )
            {

                Vector3 leftPoint = startPoint - new Vector3( ( cellSize.x + SeamSize ) * ( col + 1 ), 0, 0 );
                gameObject = Instantiate( cellPrefab, leftPoint, Quaternion.identity, cellPlane.transform );
                gameObject.transform.localScale = new Vector3( cellSize.x, cellSize.y, 0.01f );
                allCellsInScene.Add( gameObject.GetComponent<Cell>() );

                Vector3 rightPoint = startPoint + new Vector3( ( cellSize.x + SeamSize ) * ( countCols + col ), 0, 0 );
                gameObject = Instantiate( cellPrefab, rightPoint, Quaternion.identity, cellPlane.transform );
                gameObject.transform.localScale = new Vector3( cellSize.x, cellSize.y, 0.01f );
                allCellsInScene.Add( gameObject.GetComponent<Cell>() );
            }
        }
    }

    //������� ���������� "�����" �� ������
    void GenerateCells( float sizeOfViewPort )
    {
        //���������� ����� ������ � ����
        int countCols = ( int ) (sizeOfViewPort / ( cellSize.x + SeamSize ) ) + 1;
        int countRows = (int ) ( sizeOfViewPort / ( cellSize.y + SeamSize ) );


        //�������� ���������� ������ � ������ ������� ���� �������
        for( int row = 0; row < countRows; row++ )
        {
            InstantiateRow( countCols, row, sizeOfViewPort );

            //������ � ����� �������� ����� ��������� ������, ��� ����������� �������� ����
            if( row < countRows / 2  )
            {
                InstantiateRow( countCols, -row-1, sizeOfViewPort );
                InstantiateRow( countCols, row + countRows, sizeOfViewPort );
            }
        }
    }

    //������� ������� ����� �� �������� ������� �����������, ��� ��������� ������
    void CreateViewportRays( float sizeOfViewPort )
    {
        viewPortRaysList.Clear();

        viewPortRaysList.Add( new Ray( transform.position - Vector3.right * sizeOfViewPort/2  - Vector3.up* sizeOfViewPort, Vector3.up ) ); //LeftBottomToTop
        viewPortRaysList.Add( new Ray( transform.position + Vector3.right * sizeOfViewPort / 2 - Vector3.up * sizeOfViewPort, Vector3.up ) ); //RightBottomToTop
        viewPortRaysList.Add( new Ray( transform.position - Vector3.right * sizeOfViewPort / 2 + Vector3.up * sizeOfViewPort, -Vector3.up ) ); //LeftTopToBottom
        viewPortRaysList.Add( new Ray( transform.position + Vector3.right * sizeOfViewPort / 2 + Vector3.up * sizeOfViewPort, -Vector3.up ) ); //RightToBottom

        viewPortRaysList.Add( new Ray( transform.position - Vector3.right * sizeOfViewPort - Vector3.up * sizeOfViewPort /2, Vector3.right ) ); //LeftBottomToRight
        viewPortRaysList.Add( new Ray( transform.position + Vector3.right * sizeOfViewPort - Vector3.up * sizeOfViewPort / 2, -Vector3.right ) ); //RightBottomToLeft
        viewPortRaysList.Add( new Ray( transform.position - Vector3.right * sizeOfViewPort + Vector3.up * sizeOfViewPort / 2, Vector3.right ) ); //LeftTopToRight
        viewPortRaysList.Add( new Ray( transform.position + Vector3.right * sizeOfViewPort + Vector3.up * sizeOfViewPort / 2, -Vector3.right ) );//RightTopToLeft


        viewPortRaysList.Add( new Ray( transform.position - Vector3.right * sizeOfViewPort / 2 - Vector3.up * sizeOfViewPort /2 - Vector3.forward, Vector3.forward ) ); //LeftBottomCorner
        viewPortRaysList.Add( new Ray( transform.position + Vector3.right * sizeOfViewPort / 2 - Vector3.up * sizeOfViewPort / 2 - Vector3.forward, Vector3.forward ) ); //RigthBottomCorner

        viewPortRaysList.Add( new Ray( transform.position - Vector3.right * sizeOfViewPort / 2 + Vector3.up * sizeOfViewPort / 2 - Vector3.forward, Vector3.forward ) ); //LeftTopCorner
        viewPortRaysList.Add( new Ray( transform.position + Vector3.right * sizeOfViewPort / 2 + Vector3.up * sizeOfViewPort / 2 - Vector3.forward, Vector3.forward ) ); //RigthTopCorner
    }


    private void OnDrawGizmos()
    {
        viewPortRaysList.ForEach( ray =>
        {
            Gizmos.DrawLine( ray.origin, ray.origin + ray.direction * 100 );
        } );
    }

}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class CellsGenerator : MonoBehaviour
{
    Vector2 rectSize;      //Размеры отображаемой области
    [SerializeField]
    GameObject cellPrefab; //Экземпляр плитки
    [SerializeField]
    GameObject cellPlane;  //Родительский элемент плиток, через который осуществялем поворот

    [SerializeField]
    Vector2 cellSize;     //Размер плитки

    float seamSize_mm = 100;  //Ширина шва между плитками в миллиметрах
    float offsetX_mm = 10;    //Смещение рядов в миллиметрах

    public float OffsetX { get { return offsetX_mm * 0.001f;} set { offsetX_mm = value; } }
    public float SeamSize { get { return seamSize_mm * 0.001f; } set { seamSize_mm = value; }  }

    List<Ray> viewPortRaysList = new List<Ray>();
    List<Cell> allCellsInScene = new List<Cell>();
    List<RaycastHit> hits = new List<RaycastHit>();

    ///Событие возникающее при окончании расчета площади
    public event UnityAction<float> OnAreaCalculated;

    //Площадь стены
    float wallArea;

    bool shouldCalcArea;
    float time = 0.2f;
    float timer = 0.0f;
    void Update()
    {
        //Расчитываем площадь в Update после небольшой задержки
        //TODO: можно сделать в корутине
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

    //Функция расчета площади
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

    //Очищает ранее созданные плитки
    void ClearCells()
    {
        allCellsInScene.ForEach( cell => Destroy( cell.gameObject ) );
        allCellsInScene.Clear();
    }

    //Функция генерирует row ряд из countCols плиток по размерам sizeOfViewPort 
    void InstantiateRow( int countCols, int row, float sizeOfViewPort )
    {
        Vector3 startPoint = Vector3.zero;
        //Добавляем слева и справа
        for ( int col = 0; col < countCols; col++ )
        {
            //Смещение ряда относительно левой стороны, в пределах от - cellsSize.x до 0
            float offsetXN = ( OffsetX * Mathf.Abs(row) - cellSize.x ) - SeamSize;
            if ( offsetXN > 0 )
            {
                offsetXN %= cellSize.x;
                offsetXN -= cellSize.x;
            }

            //Точка инстанцинирования новой плитки
            Vector3 point = new Vector3( ( -sizeOfViewPort + cellSize.x ) / 2 + offsetXN + ( cellSize.x + SeamSize ) * col,
                ( -sizeOfViewPort + cellSize.y ) / 2 + row * ( cellSize.y + SeamSize ) - SeamSize, 0.0f );
            if ( col == 0 )
                startPoint = point;

            GameObject gameObject = Instantiate( cellPrefab, point, Quaternion.identity, cellPlane.transform );
            gameObject.transform.localScale = new Vector3( cellSize.x, cellSize.y, 0.01f );

            allCellsInScene.Add( gameObject.GetComponent<Cell>() );


            //Добавляем плитки слева и справа от основного ряда, для корректного поворота плиток
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

    //Функция генерирует "стену" из плиток
    void GenerateCells( float sizeOfViewPort )
    {
        //Количество целых плиток в ряде
        int countCols = ( int ) (sizeOfViewPort / ( cellSize.x + SeamSize ) ) + 1;
        int countRows = (int ) ( sizeOfViewPort / ( cellSize.y + SeamSize ) );


        //Начинаем выставлять плитку с левого нижнего угла направо
        for( int row = 0; row < countRows; row++ )
        {
            InstantiateRow( countCols, row, sizeOfViewPort );

            //Сверху и снизу основных рядов добавляем строки, для корректного поворота угла
            if( row < countRows / 2  )
            {
                InstantiateRow( countCols, -row-1, sizeOfViewPort );
                InstantiateRow( countCols, row + countRows, sizeOfViewPort );
            }
        }
    }

    //Функция создает лучши по границам области отображения, для отсечения плитки
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

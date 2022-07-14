using System.Collections.Generic;
using UnityEngine;
using System.Linq;


/// ����� ����� ������. �������� ���������� � ���� ����������� � �������
class CellPoint
{
    public float x { get { return position.x; } set { position.x = value; } }
    public float y { get { return position.y; }set{ position.y = value; } }
    public float z { get { return position.z; } set { position.z = value; } }
    public Vector3 position;
    public bool isInViewArea;

    public CellPoint( Vector3 pos )
    {
        position = pos;
        isInViewArea = false;
    }
}

public class Cell : MonoBehaviour
{
    //������� ������
    CellPoint leftTop = new CellPoint( Vector3.zero );
    CellPoint rightTop = new CellPoint( Vector3.zero );
    CellPoint leftBottom = new CellPoint( Vector3.zero );
    CellPoint rightBottom = new CellPoint( Vector3.zero );

    //������ ������ ������
    List<CellPoint> pointsList = new List<CellPoint>();
    //����� ������ ������
    List<Vector3> hitPoints = new List<Vector3>();
    //��� ����� ������
    List<Vector2> points = new List<Vector2>();
    //������ ������ ������ � ���������� �������
    List<Vector2> pointsInOrder = new List<Vector2>();

    //������ ������ ������
    void CalculateVertexs()
    {
        leftTop = new CellPoint( transform.position + transform.up * transform.localScale.y / 2 - transform.right * transform.localScale.x / 2 );
        rightTop = new CellPoint( transform.position + transform.up * transform.localScale.y / 2 + transform.right * transform.localScale.x / 2 );
        leftBottom = new CellPoint( transform.position - transform.up * transform.localScale.y / 2 - transform.right * transform.localScale.x / 2 );
        rightBottom = new CellPoint( transform.position - transform.up * transform.localScale.y / 2 + transform.right * transform.localScale.x / 2 );

        pointsList.Clear();
        pointsList.Add( leftBottom );
        pointsList.Add( leftTop );
        pointsList.Add( rightTop );
        pointsList.Add( rightBottom );
    }

    public void AddHitPoint( Vector3 hitPoint )
    {
        hitPoints.Add( hitPoint );
    }

    //��������� ���� �� ������� ������ ������ ������������ �������
    bool CheckPointsInRect( ref Rect rect )
    {
        leftBottom.isInViewArea = rect.Contains( leftBottom.position );
        rightBottom.isInViewArea = rect.Contains( rightBottom.position );
        leftTop.isInViewArea = rect.Contains( leftTop.position );
        rightTop.isInViewArea = rect.Contains( rightTop.position );

        return ( leftBottom.isInViewArea || rightBottom.isInViewArea || leftTop.isInViewArea || rightTop.isInViewArea );

    }

    //����������� ������� ������� ������
    public float CalculateAreaInViewPort( ref Rect rect )
    {
        float area = 0;
        CalculateVertexs();

        if( !CheckPointsInRect( ref rect ) )
        {
            return area;
        }

        area = Calculatearea();
        return area;
    }

    public void Clear()
    {
        hitPoints.Clear();
    }

    //������ ������� ������������
    float CalcTrianglearea( List<Vector2> points )
    {
        if ( points.Count != 3 ) return 0;

        float max = 0;
        int maxIndex = 0;
        List<float> sides = new List<float>( 3 );
        sides.Add( Vector2.Distance( points[0], points[1] ) );
        sides.Add( Vector2.Distance( points[1], points[2] ) );
        sides.Add( Vector2.Distance( points[0], points[2] ) );

        for( int i = 0; i < sides.Count; i++ )
        {
            if ( sides[i] > max )
            {
                max = sides[i];
                maxIndex = i;
            }
        }
        sides[maxIndex] = 1.0f;
        return ( sides[0] * sides[1] * sides[2] ) / 2;
    }

    //����������� ������� ������������� ��������������. ����� ������� ����� � ������
    float SuperficieIrregularPolygon( List<Vector2> list)
    {
        float temp = 0;
        int i = 0;
        for ( ; i < list.Count; i++ )
        {
            if ( i != list.Count - 1 )
            {
                float mulA = list[i].x * list[i + 1].y;
                float mulB = list[i + 1].x * list[i].y;
                temp = temp + ( mulA - mulB );
            }
            else
            {
                float mulA = list[i].x * list[0].y;
                float mulB = list[0].x * list[i].y;
                temp = temp + ( mulA - mulB );
            }
        }
        temp *= 0.5f;
        return Mathf.Abs( temp );
    }

    //������ ������� ������
    float Calculatearea()
    {
        float area = 0.0f;

        //���� ����� ������ ��������� � ��������� ������, �� ������ ����� ������
        for ( int i = 0; i < pointsList.Count; i++ )
        {
            
            for ( int j = 0; j < hitPoints.Count; j++ )
            {
                if ( Vector2.Distance( hitPoints[j], pointsList[i].position ) < 0.001f )
                {
                    hitPoints.RemoveAt( j );
                    j--;
                }
            }
        }

        //��������� ����� ��� ������� �������
        List<CellPoint> inPoints = pointsList.Where( p => p.isInViewArea ).ToList<CellPoint>();
        points = new List<Vector2>();
        inPoints.ForEach( p => points.Add( new Vector2( p.x, p.y ) ) );
        hitPoints.ForEach( p => points.Add( new Vector2( p.x, p.y ) ) );

        if( points.Count < 3 )
        {
            return 0.0f;
        }

        //���� ������ ������������ ������� ������ ��� ����� ������� ������� ��� � ������������
        if ( points.Count == 3)
        {
            area = CalcTrianglearea( points );
        }
        else
        {
            //������ ������ ������ � ���������� �������
            pointsInOrder.Clear();
            pointsInOrder.AddRange( points );
            ClockwiseComparer comparer = new ClockwiseComparer( new Vector2( transform.position.x, transform.position.y ) );
            pointsInOrder.Sort( comparer );

            area = SuperficieIrregularPolygon( pointsInOrder );
        }
        return area;
    }
}

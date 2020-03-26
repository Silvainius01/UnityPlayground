using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

[System.Serializable]
public class HexTile
{
    public int layer;
    public int tileNum;
    public Vector2 pos;
    [HideInInspector]
    public HexTile[] connections = new HexTile[6];

    public HexTile(Vector2 position, int layer, int tileNum)
    {
        pos = position;
        this.layer = layer;
        this.tileNum = tileNum;
    }

    public static void Connect(int cIndex, HexTile firstTile, HexTile secondTile)
    {
        firstTile.connections[cIndex] = secondTile;
        secondTile.connections[GetOppIndex(cIndex)] = firstTile;
    }

    static int GetOppIndex(int index) { return (index + 3) % 6; }

    public HexTile CreateConnectedTile(int cIndex, int tileNum)
    {
        double ang = ((Mathc.TWO_PI / 6) * cIndex) + Mathc.HALF_PI; // Yay radians
        Vector2 pos = new Vector2((float)Math.Cos(ang), (float)Math.Sin(ang));
        HexTile newTile = new HexTile(pos, this.layer + 1, tileNum);
        Connect(cIndex, this, newTile);
        return newTile;
    }

    public string GetConnectionSummary()
    {
        StringBuilder msg = new StringBuilder($"\n {tileNum}[{layer}]: ");

        for (int i = 0; i < 6; ++i)
        {
            if (connections[i] == null)
            {
                msg.Append($"[{i}, null] ");
                continue;
            }

            Vector2 nPos = connections[i].pos - pos;
            float ang = Mathf.Atan2(nPos.y, nPos.x);
            ang = Mathc.AnglePiToAngle2Pi(ang) * Mathf.Rad2Deg;
            msg.Append($"[{i}, {ang.ToString("F1")}, {nPos.ToString("F2")}] ");
        }

        return msg.ToString();
    }
}

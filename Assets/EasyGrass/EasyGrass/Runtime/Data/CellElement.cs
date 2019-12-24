using UnityEngine;

namespace EasyFramework.Grass.Runtime
{
    /*
    public struct CellElement
    {
        public readonly Vector3 position;
        public readonly Vector3 normal;
        public readonly Vector3 scale;

        public CellElement(Vector3 position, Vector3 normal, Vector3 scale)
        {
            this.position = position;
            this.normal = normal;
            this.scale = scale;
        }

        public override string ToString()
        {
            return "\n" + position.ToString() + "\n" + normal.ToString() + "\n" + scale.ToString();
        }
    }
    */

    public struct CellElement
    {
        public readonly Vector3 position;

        public CellElement(Vector3 position)
        {
            this.position = position;
        }

        public override string ToString()
        {
            return "\n" + position.ToString() + "\n";
        }
    }
}
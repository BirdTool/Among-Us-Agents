using UnityEngine;

namespace AMG.UI.Settings
{
    public sealed class SettingsLayout
    {
        public const float ColumnX = 1.3f;
        private const float FirstRowY = 1.8f;
        private const float RowHeight = 0.5f;
        private const float Z = -6f;

        private int _row;
        private int _column;   // 0 = left cell free, 1 = right cell free, 2 = row is full

        public Vector3 NextCell()
        {
            if (_column > 1)
            {
                _row++;
                _column = 0;
            }

            var position = new Vector3(_column == 0 ? -ColumnX : ColumnX, FirstRowY - RowHeight * _row, Z);
            _column++;
            return position;
        }

        // The centre of the next free row
        public Vector3 NextRow()
        {
            if (_column > 0)
            {
                _row++;
                _column = 0;
            }

            var position = new Vector3(0f, FirstRowY - RowHeight * _row, Z);
            _column = 2;
            return position;
        }
    }
}

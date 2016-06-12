using System;

namespace HexMage.GUI
{
    class Either<L, R>
    {
        private readonly R _right;
        private readonly L _left;
        private readonly bool _isLeft;

        public Either(L left) {
            _left = left;
            _isLeft = true;
        }

        private Either(R right) {
            _right = right;
            _isLeft = false;
        }

        public static Either<L, R> Left(L left) {
            return new Either<L, R>(left);
        }

        public static Either<L, R> Right(R right) {
            return new Either<L, R>(right);
        }

        public bool IsLeft => _isLeft;
        public bool IsRight => !_isLeft;

        public L LeftValue {
            get {
                if (!_isLeft)
                    throw new InvalidOperationException("Accessing Left value of an Either that is Right is invalid.");
                return _left;
            }
        }

        public R RightValue {
            get {
                if (_isLeft)
                    throw new InvalidOperationException("Accessing Right value of an Either that is Left is invalid.");
                return _right;
            }
        }
    }
}
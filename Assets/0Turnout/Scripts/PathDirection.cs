using FluffyUnderware.Curvy;
using FluffyUnderware.Curvy.Controllers;

/// <summary>
/// 使い方は二つ
/// <see cref="ConnectionSwitch"/>のToDirection的の使い方は、<see cref="controlPoint"/>から<see cref="movementDirection"/>へ進むの情報             〇-----〇→---〇
/// <see cref="ConnectionSwitch"/>のFromDirection的の使い方は、<see cref="movementDirection"/>へ進んで<see cref="controlPoint"/>に到達する情報     〇---→〇-----〇
/// この情報で次の移動先のPathDirectionを取得できる
/// </summary>
public struct PathDirection
{
    /// <summary>
    /// コントロールポイント
    /// </summary>
    public CurvySplineSegment controlPoint;
    /// <summary>
    /// 移動方向
    /// </summary>
    public MovementDirection movementDirection;

    public PathDirection(CurvySplineSegment controlPoint, MovementDirection movementDirection)
    {
        this.controlPoint = controlPoint;
        this.movementDirection = movementDirection;
    }

    /// <summary>
    /// SplineControllerで初期化
    /// controlPointが既に知っていれば入力した方がいい。丁度ポイントにいる場合、Spline.TFToSegmentによる変換はFloat精度によって前のポイントを計算し出す可能性があります
    /// </summary>
    public PathDirection(SplineController splineController, CurvySplineSegment controlPoint = null)
    {
        if (controlPoint != null)
            this.controlPoint = controlPoint;
        else
            this.controlPoint = splineController.Spline.TFToSegment(splineController.RelativePosition);
        this.movementDirection = splineController.MovementDirection;
        // SplineControllerは丁度ControlPointにいるではない場合、前方のControlPointを使う
        if (movementDirection == MovementDirection.Forward && this.controlPoint.TF != splineController.RelativePosition)
            this = GetNextPathDirection();
    }

    /// <summary>
    /// 移動方向を逆方向に                                                             〇-----〇→---〇 は 〇---←〇-----〇 に
    /// 分岐の<see cref="ConnectionSwitch"/>のFromDirectionとToDirectionの相互変換用   〇-----〇→---〇 は 〇-----〇←---〇 に
    /// </summary>
    public PathDirection Reverse()
    {
        return new PathDirection(controlPoint, movementDirection == MovementDirection.Forward ? MovementDirection.Backward : MovementDirection.Forward);
    }

    /// <summary>
    /// このcontrolPointからmovementDirection方向移動して、次のPathDirectionを取得
    /// 〇-----〇→---〇 は 〇-----〇-----〇→ に
    /// </summary>
    public PathDirection GetNextPathDirection()
    {
        PathDirection nextPathDirection = this;
        if (controlPoint.FollowUp != null &&
            ((controlPoint.IsFirstControlPoint && movementDirection == MovementDirection.Backward)
          || (controlPoint.IsLastControlPoint && movementDirection == MovementDirection.Forward)))
        {
            switch (controlPoint.FollowUpHeading.ResolveAuto(controlPoint.FollowUp))
            {
                case ConnectionHeadingEnum.Minus:
                    nextPathDirection.movementDirection = MovementDirection.Backward;
                    break;
                case ConnectionHeadingEnum.Sharp:
                    break;
                case ConnectionHeadingEnum.Plus:
                    nextPathDirection.movementDirection = MovementDirection.Forward;
                    break;
                default:
                    throw new System.ArgumentOutOfRangeException();
            }
            nextPathDirection.controlPoint = controlPoint.FollowUp;
        }
        else if (movementDirection == MovementDirection.Forward)
        {
            nextPathDirection.controlPoint = controlPoint.Spline.GetNextControlPoint(controlPoint);
        }
        else
        {
            nextPathDirection.controlPoint = controlPoint.Spline.GetPreviousControlPoint(controlPoint);
        }
        return nextPathDirection;
    }

    /// <summary>
    /// <see cref="GetNextPathDirection"/>の距離取得バージョン
    /// </summary>
    public PathDirection GetNextPathDirection(out float distanceSegment)
    {
        if (controlPoint == null)
            throw new System.ArgumentOutOfRangeException();
        PathDirection nextPathDirection = this;
        distanceSegment = 0;
        if (controlPoint.FollowUp != null &&
            ((controlPoint.IsFirstControlPoint && movementDirection == MovementDirection.Backward)
          || (controlPoint.IsLastControlPoint && movementDirection == MovementDirection.Forward)))
        {
            switch (controlPoint.FollowUpHeading.ResolveAuto(controlPoint.FollowUp))
            {
                case ConnectionHeadingEnum.Minus:
                    nextPathDirection.movementDirection = MovementDirection.Backward;
                    break;
                case ConnectionHeadingEnum.Sharp:
                    break;
                case ConnectionHeadingEnum.Plus:
                    nextPathDirection.movementDirection = MovementDirection.Forward;
                    break;
                default:
                    throw new System.ArgumentOutOfRangeException();
            }
            nextPathDirection.controlPoint = controlPoint.FollowUp;
        }
        else if (movementDirection == MovementDirection.Forward)
        {
            nextPathDirection.controlPoint = controlPoint.Spline.GetNextControlPoint(controlPoint);
            if (nextPathDirection.controlPoint != null)
                distanceSegment = nextPathDirection.controlPoint.Distance - controlPoint.Distance;
        }
        else
        {
            nextPathDirection.controlPoint = controlPoint.Spline.GetPreviousControlPoint(controlPoint);
            if (nextPathDirection.controlPoint != null)
                distanceSegment = controlPoint.Distance - nextPathDirection.controlPoint.Distance;
        }
        return nextPathDirection;
    }

    public CurvyConnection GetNextConnection(bool doMoveThisToo = false, bool includeSelfConnection = false, int searchTimesLimit = 30)
    {
        PathDirection pathDirection = this;
        for (int loopCount = 0; pathDirection.controlPoint != null && loopCount <= searchTimesLimit; loopCount++)
        {
            // パスの繋がりを取得
            if (pathDirection.controlPoint.Connection != null && (includeSelfConnection == true || loopCount > 0))
            {
                if (doMoveThisToo == true)
                    this = pathDirection;
                return pathDirection.controlPoint.Connection;
            }
            // 一周して自身に戻った
            else if (pathDirection == this && loopCount > 0)
            {
                return null;
            }
            // 次のポイントを確認
            pathDirection = pathDirection.GetNextPathDirection();
        }
        return null;
    }

    /// <summary>
    /// 入力PathDirectionを前に移動して、このPathDirectionに届けるかを確認
    /// </summary>
    /// <param name="pathDirectionToSearch">検索したいPathDirection</param>
    /// <param name="searchTimesLimit">検索回数制限</param>
    /// <returns>True:届ける False:届けない/検索回数制限超え</returns>
    public bool CouldBeReachedWithoutSwitch(PathDirection pathDirectionToSearch, int searchTimesLimit = 100)
    {
        PathDirection pathDirection = pathDirectionToSearch;
        int i = 0;
        while (pathDirection.controlPoint != null)
        {
            // 到達
            if (pathDirection == this)
            {
                return true;
            }
            // 到達する前に、使える分岐が存在する場合、到達できないと判断する
            else if (pathDirection.controlPoint.Connection != null && i > 0)
            {
                // 全ての分岐の方向を確認
                var connectionSwitch = pathDirection.controlPoint.Connection.GetComponent<ConnectionSwitch>();
                if (connectionSwitch?.AvailableDirection.Count > 0)
                {
                    foreach (var pathDirectionTuple in connectionSwitch.AvailableDirection)
                    {
                        if (pathDirectionTuple.fromDirection == pathDirection)
                        {
                            return false;
                        }
                    }
                }
            }
            // 一周して自身に戻った
            else if (pathDirection == pathDirectionToSearch && i > 0)
            {
                return false;
            }
            // 検索回数制限
            if (++i >= searchTimesLimit)
            {
                //UnityEngine.Debug.LogWarning("パス方向の検索回数制限到達!");
                return false;
            }
            // 次のポイントを確認
            pathDirection = pathDirection.GetNextPathDirection();
        }
        return false;
    }

    /// <summary>
    /// 前方のメタデータを確認
    /// </summary>
    /// <typeparam name="T">メタデータ</typeparam>
    /// <param name="searchTimesLimit">検索回数制限</param>
    /// <returns>メタデータ</returns>
    public T GetNextMetadata<T>(out float distance, int searchTimesLimit = 5) where T : CurvyMetadataBase
    {
        PathDirection pathDirection = this;
        distance = 0;
        var position = pathDirection.controlPoint.Distance;
        for (int loopTime = 0; pathDirection.controlPoint != null && loopTime <= searchTimesLimit; loopTime++)      //検索回数制限
        {
            // 有効なメタデータが存在
            var metadata = pathDirection.controlPoint.GetMetadata<T>();
            if (metadata?.isActiveAndEnabled == true)
            {
                return metadata;
            }
            // 到達する前に、使える分岐が存在する場合、到達できないと判断する
            else if (pathDirection.controlPoint.Connection != null)
            {
                // 全ての分岐の方向を確認
                var connectionSwitch = pathDirection.controlPoint.Connection.GetComponent<ConnectionSwitch>();
                if (connectionSwitch?.enabled == true && connectionSwitch?.AvailableDirection.Count > 0)
                {
                    foreach (var pathDirectionTuple in connectionSwitch.AvailableDirection)
                    {
                        if (pathDirectionTuple.fromDirection == pathDirection)
                        {
                            return null;
                        }
                    }
                }
            }
            // 一周して自身に戻った
            else if (pathDirection == this && loopTime > 0)
            {
                return null;
            }
            // 次のポイントを確認
            pathDirection = pathDirection.GetNextPathDirection(out float distanceSegment);
            distance += distanceSegment;
        }
        return null;
    }

    public static bool operator ==(PathDirection p1, PathDirection p2)
    {
        return p1.Equals(p2);
    }
    public static bool operator !=(PathDirection p1, PathDirection p2)
    {
        return !p1.Equals(p2);
    }
}
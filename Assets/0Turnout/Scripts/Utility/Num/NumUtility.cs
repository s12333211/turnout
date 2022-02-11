using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using UnityEngine;

public static class NumUtility
{
    public enum NumType {
        abc,
        kmg,
        comma,
        none
    }

    public const string unit1 = "abcdefghijklmnopqrstuvwxyz";
    public const string unit2 = "KMGTPEZY";

    private static string defaultUnit = unit1;

    public static void SetDefaultUnit(string unit) {
        NumUtility.defaultUnit = unit;
    }

    public static string MakeCommaNum(long target) {
        return target.ToString("#,##0");
    }

    public static string MakeCommaNum(double target) {
        return target.ToString("#,##0");
    }

    public static string MakeCommaNum(BigInteger target) {
        return target.ToString("#,##0");
    }

    public static string MakeDispNum(double target, NumType numType) {

        switch (numType) {
            case NumType.abc:
                return MakeDispNum(target, unit1);
            case NumType.kmg:
                return MakeDispNum(target, unit2);
            case NumType.comma:
                return MakeCommaNum(target);
            case NumType.none:
            default:
                return target.ToString();
        }
    }

    public static string MakeDispNum(BigInteger target, NumType numType) {

        switch (numType) {
            case NumType.abc:
                return MakeDispNum(target, unit1);
            case NumType.kmg:
                return MakeDispNum(target, unit2);
            case NumType.comma:
                return MakeCommaNum(target);
            case NumType.none:
            default:
                return target.ToString();
        }
    }

    public static string MakeDispNum(double target, string _unit = null) {
        return MakeDispNum(new BigInteger(target), _unit);
    }

    public static string MakeDispNum(BigInteger target, string _unit = null) {

        string unit = _unit;
        if (unit == null) {
            unit = defaultUnit;
        }

        if (target < 1000) {
            return target.ToString();
        }

        int cnt = -1;
        BigInteger num = BigInteger.Parse(target.ToString());
        BigInteger tmp = BigInteger.Parse(num.ToString());
        while (0 < tmp) {
            tmp = BigInteger.Parse((num / 1000).ToString());
            if (0 < tmp) {
                cnt++;
                num = tmp;
            }
        }

        string disp = target.ToString("#,0").Substring(0, 5);
        if (cnt < unit.Length) {
            disp += unit.Substring(cnt, 1);
        } else {
            int unit1 = cnt / unit.Length - 1;
            int unit2 = cnt % unit.Length;

            disp += unit.Substring(unit1, 1);
            disp += unit.Substring(unit2, 1);
        }
        disp = disp.Replace(",", ".");

        return disp;
    }
}

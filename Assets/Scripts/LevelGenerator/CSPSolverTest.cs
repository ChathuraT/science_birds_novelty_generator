using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//using Decider.Csp.BaseTypes;
//using Decider.Csp.Integer;
//using Decider.Csp.Global;

public class CSPSolverTest
{
    public CSPSolverTest()
    {
        /*
        int divider = 1;
        var a = new VariableInteger("0_0", -350 / divider, 900 / divider);
        var b = new VariableInteger("0_1", -350 / divider, 900 / divider);
        var c = new VariableInteger("0_2", -350 / divider, 900 / divider);
        var d = new VariableInteger("1_0", -350 / divider, 900 / divider);
        var e = new VariableInteger("1_1", -350 / divider, 900 / divider);
        var f = new VariableInteger("1_2", -350 / divider, 900 / divider);
        var i = new VariableInteger("2_0", -350 / divider, 900 / divider);
        var j = new VariableInteger("2_1", -350 / divider, 900 / divider);
        var k = new VariableInteger("2_2", -350 / divider, 900 / divider);
        var l = new VariableInteger("3_0", -350 / divider, 900 / divider);
        var m = new VariableInteger("3_1", -350 / divider, 900 / divider);
        var n = new VariableInteger("3_2", -350 / divider, 900 / divider);
        var o = new VariableInteger("4_0", -350 / divider, 900 / divider);
        var p = new VariableInteger("4_1", -350 / divider, 900 / divider);
        var q = new VariableInteger("4_2", -350 / divider, 900 / divider);
        var constraints = new List<IConstraint>{
           
            //new ConstraintInteger(a + 22 == b),
            //new ConstraintInteger(b + 22 == c),
            //new ConstraintInteger(d + 22 == e),
            //new ConstraintInteger(e + 22 == f),
            new ConstraintInteger(d == n),
            new ConstraintInteger(k <= d),
            new ConstraintInteger(i + 23 == j),
            new ConstraintInteger(j + 23 == k),
            new ConstraintInteger(i == q),
            //new ConstraintInteger(l + 10 == m),
            //new ConstraintInteger(m + 10 == n),
            new ConstraintInteger(o + 10 == p),
            new ConstraintInteger(p + 10 == q)
            
        };

        ConstraintInteger CI = new ConstraintInteger(k <= d);
        Debug.Log("checking ConstraintInteger CI, left: " + CI.Left + " right: " + CI.Right + " Integer: " + CI.Integer );

        var variables = new[] { o, p, q, a, b, c, d, e, f, i, j, k, l, m, n };
        var state = new StateInteger(variables, constraints);
        // var searchResult = state.Search();

        foreach (VariableInteger variable in variables)
        {
            Debug.Log("variable: " + variable.Name + " value: " + variable);
        }


        Debug.Log($"Runtime:\t{state.Runtime}\nBacktracks:\t{state.Backtracks}\n");


    */

        // variables
        int divider = 1;
        var a = new Variable("0_0", -350 / divider, 900 / divider);
        var b = new Variable("0_1", -350 / divider, 900 / divider);
        var c = new Variable("0_2", -350 / divider, 900 / divider);

        var d = new Variable("1_0", -350 / divider, 900 / divider);
        var e = new Variable("1_1", -350 / divider, 900 / divider);
        var f = new Variable("1_2", -350 / divider, 900 / divider);

        var i = new Variable("2_0", -350 / divider, 900 / divider);
        var j = new Variable("2_1", -350 / divider, 900 / divider);
        var k = new Variable("2_2", -350 / divider, 900 / divider);

        var l = new Variable("3_0", -350 / divider, 900 / divider);
        var m = new Variable("3_1", -350 / divider, 900 / divider);
        var n = new Variable("3_2", -350 / divider, 900 / divider);

        var o = new Variable("4_0", -350 / divider, 900 / divider);
        var p = new Variable("4_1", -350 / divider, 900 / divider);
        var q = new Variable("4_2", -350 / divider, 900 / divider);

        // constraints
        var constraints = new List<Expression>
        {
            new Expression(a, "+", 22, "==", b),
            new Expression(b, "+", 22, "==", c),
            new Expression(d, "+", 22, "==", e),
            new Expression(e, "+", 22, "==", f),
            new Expression(i, "+", 23,"==", j),
            new Expression(j, "+", 23, "==", k),
            new Expression(l, "+", 10,"==", m),
            new Expression(m, "+", 10, "==", n),
            new Expression(o, "+", 10, "==", p),
            new Expression(p, "+", 10, "==", q),

            new Expression(d, "==", n),

            new Expression(i, "<", d),
            new Expression(j, "<", d),
            new Expression(k, "<=", d),
            new Expression(i, "<", e),
            new Expression(j, "<", e),
            new Expression(k, "<", e),
            new Expression(i, "<", f),
            new Expression(j, "<", f),
            new Expression(k, "<", f),

            new Expression(i, "<", n),
            new Expression(j, "<", n),
            new Expression(k, "<=", n),

            new Expression(i, "==", q)
        };

        var variables = new List<Variable> { a, b, c, d, e, f, i, j, k, l, m, n, o, p, q };

        CSP cspSolver = new CSP(variables, constraints, "x");
        cspSolver.Solve();

    }
}

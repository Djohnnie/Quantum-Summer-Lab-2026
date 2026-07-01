// ============================================================
// CHALLENGE METADATA
// Edit the fields below directly. SolutionTemplate and
// VerificationTemplate are NOT stored here - they are derived
// automatically from the code below (see the SOLVE markers).
// After editing, run `python _challenges/generate_challenges.py`
// to regenerate QuantumSummerLab.Tools/Challenges.cs.
// ============================================================
// Name = "D2"
// Title = "Generate generalized W-state"
// Description = "Your task is to create a generalized W state on n qubits where n = 2^k (1 ≤ k ≤ 4) from zero |0..0⟩ state.[BR]The classic W-state is defined as |W⟩ = 1/√3 (|100⟩ + |010⟩ + |001⟩) for n = 3, which is a helpful reference but is not itself a valid input for this challenge since 3 is not a power of 2.[BR]The generalized W-state you need to implement is defined as |W⟩ = 1/√n (|10..0⟩ + |01..0⟩ + ... + |00..1⟩), where n is always a power of 2 in the range n = 2^k (1 ≤ k ≤ 4), i.e. n ∈ {2, 4, 8, 16}.[BR]You have to implement the Solve operation which takes an array of n qubits in state |0..0⟩ and you need to create the W-state on them.[BR]The operation should have the following signature:"
// Tldr = "You should implement the empty Solve operation below and prepare a generalized W-state on the provided 2^k (1 ≤ k ≤ 4) qubits."
// ExampleDescription = ""
// ExampleCode = ""
// ExpectedOutput = ""
// ===EXPECTED-STATES-START===
// [
//   {
//     "id": "|0001⟩",
//     "amplitudeReal": 0.5,
//     "amplitudeImaginary": 0
//   },
//   {
//     "id": "|0010⟩",
//     "amplitudeReal": 0.5,
//     "amplitudeImaginary": 0
//   },
//   {
//     "id": "|0100⟩",
//     "amplitudeReal": 0.5,
//     "amplitudeImaginary": 0
//   },
//   {
//     "id": "|1000⟩",
//     "amplitudeReal": 0.5,
//     "amplitudeImaginary": 0
//   }
// ]
// ===EXPECTED-STATES-END===
// CopilotInstructions = ""
// Level = 4
// ============================================================

import Std.Arithmetic.*;
import Std.Canon.*;
import Std.Diagnostics.*;
import Std.Math.*;
import Std.Measurement.*;

operation Main() : Unit
{
    use qs = Qubit[4];
    Solve(qs);
    DumpRegister(qs);
    ResetAll(qs);
}

// ===SOLVE-START===
operation Solve (qs : Qubit[]) : Unit
{
    let n = Length(qs);

    if( n == 1 )
    {
        X(qs[0]);
    }
    else
    {
        let k = n / 2;

        Solve(qs[0..k-1]);

        use a = Qubit();
        H(a);

        for i in 0 .. k - 1
        {
            Controlled SWAP([a], (qs[i], qs[i+k]));
        }

        for i in k .. n - 1
        {
            CNOT(qs[i], a);
        }
    }
}
// ===SOLVE-END===

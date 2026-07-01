// ============================================================
// CHALLENGE METADATA
// Edit the fields below directly. SolutionTemplate and
// VerificationTemplate are NOT stored here - they are derived
// automatically from the code below (see the SOLVE markers).
// After editing, run `python _challenges/generate_challenges.py`
// to regenerate QuantumSummerLab.Tools/Challenges.cs.
// ============================================================
// Name = "C1"
// Title = "Generate GHZ state"
// Description = "Your task is to create a Greenberger–Horne–Zeilinger (GHZ) state on n qubits (1 ≤ n ≤ 8) in zero |0..0⟩ state.[BR]The GHZ state is defined as |GHZ⟩ = 1/√2 (|0..0⟩ + |1..1⟩).[BR]You have to implement an operation which takes an array of n qubits and you need to create the GHZ state on them. The operation should have the following signature:"
// Tldr = "You should implement the empty Solve operation below and prepare the Greenberger–Horne–Zeilinger (GHZ) state on the provided qubits."
// ExampleDescription = ""
// ExampleCode = ""
// ExpectedOutput = "true"
// ===EXPECTED-STATES-START===
// [
//   {
//     "id": "|00000000⟩",
//     "amplitudeReal": 0.7071,
//     "amplitudeImaginary": 0
//   },
//   {
//     "id": "|11111111⟩",
//     "amplitudeReal": 0.7071,
//     "amplitudeImaginary": 0
//   }
// ]
// ===EXPECTED-STATES-END===
// CopilotInstructions = ""
// Level = 3
// ============================================================

import Std.Arithmetic.*;
import Std.Canon.*;
import Std.Diagnostics.*;
import Std.Math.*;
import Std.Measurement.*;

operation Main() : Bool
{
    mutable resultCounter = 0;

    for i in 1..8
    {
        let result = CheckOperationsAreEqual(i, Solve, Expected);
        set resultCounter += result ? 1 | 0;
    }

    LogMessage(resultCounter == 8, "You have successfully generated the GHZ state", "You have failed to generate the GHZ state");

    use qs = Qubit[8];
    Solve(qs);
    DumpRegister(qs);
    ResetAll(qs);

    return resultCounter == 8;
}

function LogMessage(isValid: Bool, validMessage: String, invalidMessage: String) : ()
{
    let message = "{\"valid\": " + (isValid ? "true" | "false") + ", \"message\": \"" + (isValid ? validMessage | invalidMessage) + "\"}";
    Message(message);
}

operation Expected (qs : Qubit[]) : Unit is Adj
{
    H(qs[0]);
    for i in 1 .. Length(qs) - 1
    {
        CNOT(qs[0], qs[i]);
    }
}

// ===SOLVE-START===
operation Solve (qs : Qubit[]) : Unit
{
    H(qs[0]);
    for i in 1 .. Length(qs) - 1
    {
        CNOT(qs[0], qs[i]);
    }
}
// ===SOLVE-END===

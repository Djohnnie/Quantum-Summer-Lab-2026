// ============================================================
// CHALLENGE METADATA
// Edit the fields below directly. SolutionTemplate and
// VerificationTemplate are NOT stored here - they are derived
// automatically from the code below (see the SOLVE markers).
// After editing, run `python _challenges/generate_challenges.py`
// to regenerate QuantumSummerLab.Tools/Challenges.cs.
// ============================================================
// Name = "C3"
// Title = "Increment"
// Description = "Implement an operation on a register of n qubits that increments the number written in the register modulo 2^n.[BR]Your operation should take an array of qubits that encodes an unsigned integer in little-endian format, with the least significant bit written first (corresponding to the array element with index 0).[BR]The Solve operation should take the input register and change it without measuring it in order to keep the quantum state, but increment the values its state represents.[BR]For example:[BR]1/2(|0001⟩ + |0010⟩ + |0100⟩ + |1000⟩) should be incremented to 1/2(|1001⟩ + |1010⟩ + |1100⟩ + |0100⟩)[BR]The solve operation should have the following signature:"
// Tldr = "You should implement the empty Solve operation below and increment the number encoded (in little-endian format) in the provided register of n qubits modulo 2^n."
// ExampleDescription = ""
// ExampleCode = ""
// ExpectedOutput = "true"
// ===EXPECTED-STATES-START===
// [
//   {
//     "id": "|1001⟩",
//     "amplitudeReal": 0.5,
//     "amplitudeImaginary": 0
//   },
//   {
//     "id": "|1010⟩",
//     "amplitudeReal": 0.5,
//     "amplitudeImaginary": 0
//   },
//   {
//     "id": "|1100⟩",
//     "amplitudeReal": 0.5,
//     "amplitudeImaginary": 0
//   },
//   {
//     "id": "|0100⟩",
//     "amplitudeReal": 0.5,
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

    for i in 1 .. 8
    {
        let result = CheckOperationsAreEqual(i, Solve, Expected);
        set resultCounter += result ? 1 | 0;
    }

    LogMessage(resultCounter == 8, "You have implemented an increment operation successfully", "You have failed to implement an increment operation");

    // Prepare a generalized W-state on 4 qubits.
    use qs = Qubit[4];
    PrepareInitialState(qs);

    // Call the Solve operation to increment the number in the register modulo 2^n.
    Solve(qs);

    // Dump the register to evaluate the results.
    DumpRegister(qs);

    // Reset the qubits to their initial state.
    MResetEachZ(qs);

    return resultCounter == 8;
}

operation PrepareInitialState (qs : Qubit[]) : Unit
{
    let n = Length(qs);

    if( n == 1 )
    {
        X(qs[0]);
    }
    else
    {
        let k = n / 2;

        PrepareInitialState(qs[0..k-1]);

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

function LogMessage(isValid: Bool, validMessage: String, invalidMessage: String) : ()
{
    let message = "{\"valid\": " + (isValid ? "true" | "false") + ", \"message\": \"" + (isValid ? validMessage | invalidMessage) + "\"}";
    Message(message);
}

operation Expected (register : Qubit[]) : Unit is Adj + Ctl
{
    if (Length(register) > 1)
    {
        (Controlled Expected)([register[0]], register[1...]);
    }

    X(register[0]);
}

// ===SOLVE-START===
operation Solve (register : Qubit[]) : Unit is Adj + Ctl
{
    // Only if the register contains at least two qubits
    if (Length(register) > 1)
    {
        // Increment the rest of the number if the least significant bit is 1.
        // This is done by recursively applying the controlled Solve operation to the rest of the register.
        (Controlled Solve)([register[0]], register[1...]);
    }

    // Increment the least significant bit.
    X(register[0]);
}
// ===SOLVE-END===

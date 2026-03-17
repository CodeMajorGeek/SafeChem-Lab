from __future__ import annotations

import base64
import os
from pathlib import Path

from cryptography.hazmat.primitives import serialization
from cryptography.hazmat.primitives.asymmetric import rsa


def to_base64_uint(value: int) -> str:
    size = (value.bit_length() + 7) // 8
    return base64.b64encode(value.to_bytes(size, "big")).decode("ascii")


def main() -> None:
    root = Path(__file__).resolve().parents[1]
    keys_dir = root / "keys"
    keys_dir.mkdir(parents=True, exist_ok=True)

    unity_security_dir = Path(__file__).resolve().parents[3] / "Assets" / "Resources" / "Security"
    unity_security_dir.mkdir(parents=True, exist_ok=True)

    private_key = rsa.generate_private_key(public_exponent=65537, key_size=2048)
    public_key = private_key.public_key()
    numbers = public_key.public_numbers()

    private_pem = private_key.private_bytes(
        encoding=serialization.Encoding.PEM,
        format=serialization.PrivateFormat.PKCS8,
        encryption_algorithm=serialization.NoEncryption(),
    )
    public_pem = public_key.public_bytes(
        encoding=serialization.Encoding.PEM,
        format=serialization.PublicFormat.SubjectPublicKeyInfo,
    )

    xml_key = (
        "<RSAKeyValue>"
        f"<Modulus>{to_base64_uint(numbers.n)}</Modulus>"
        f"<Exponent>{to_base64_uint(numbers.e)}</Exponent>"
        "</RSAKeyValue>"
    )

    (keys_dir / "private_key.pem").write_bytes(private_pem)
    (keys_dir / "public_key.pem").write_bytes(public_pem)
    (keys_dir / "public_key.xml").write_text(xml_key, encoding="ascii")

    (unity_security_dir / "game_public_key.xml").write_text(xml_key, encoding="ascii")

    print("Keys generated.")
    print(f"- API private key: {keys_dir / 'private_key.pem'}")
    print(f"- API public key: {keys_dir / 'public_key.pem'}")
    print(f"- Unity public key: {unity_security_dir / 'game_public_key.xml'}")


if __name__ == "__main__":
    os.environ.setdefault("PYTHONIOENCODING", "utf-8")
    main()

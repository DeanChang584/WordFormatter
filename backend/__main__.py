"""Entry point for PyInstaller-packaged backend."""
import uvicorn
from backend.server import HOST, PORT

uvicorn.run(
    "backend.server:app",
    host=HOST,
    port=PORT,
    log_level="info",
)

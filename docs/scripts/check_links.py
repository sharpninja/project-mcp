#!/usr/bin/env python3
"""
Crawl built Jekyll _site: start from root, follow all same-origin links,
report broken links (missing file or missing anchor).
"""
import os
import re
from pathlib import Path
from urllib.parse import urljoin, urlparse, unquote

SITE_DIR = Path(__file__).resolve().parent.parent / "_site"
BASEURL = "/project-mcp/"


def hrefs_from_html(filepath: Path) -> list[tuple[str, str]]:
    """Extract (href, link_text) from <a href="..."> in HTML file."""
    text = filepath.read_text(encoding="utf-8", errors="replace")
    # Match <a href="..." ...> or <a ... href="...">
    pattern = re.compile(r'<a\s+[^>]*href=(["\'])([^"\']+)\1[^>]*>', re.IGNORECASE)
    out = []
    for m in pattern.finditer(text):
        out.append((m.group(2).strip(), ""))
    return out


def same_origin(href: str) -> bool:
    if not href or href.startswith(("#", "mailto:", "tel:")):
        return True  # in-page or special, we'll resolve
    if href.startswith(("http://", "https://")):
        return False
    return True


def path_from_href(href: str, current_page_path: str) -> str | None:
    """
    Resolve href (from a page at current_page_path) to a local file path
    relative to _site (e.g. 'index.html', '03-data-model.html').
    Returns None if external.
    """
    if not href or href.startswith("mailto:") or href.startswith("tel:"):
        return None
    if href.startswith("#"):
        # Same page anchor
        return current_page_path
    if href.startswith(("http://", "https://")):
        return None
    # Resolve relative to base
    if href.startswith(BASEURL):
        path = href[len(BASEURL):].split("#")[0].rstrip("/")
        return f"{path}/index.html" if not path or path.endswith("/") else (path if path.endswith(".html") else f"{path}.html")
    # Relative link from current page
    base = os.path.dirname(current_page_path)
    joined = urljoin(base + "/" if base else "/", href)
    if joined.startswith("/"):
        joined = joined.lstrip("/")
    if joined.startswith(BASEURL.strip("/")):
        joined = joined[len(BASEURL.strip("/")):]
    # Strip anchor
    joined = joined.split("#")[0].rstrip("/")
    if not joined:
        return "index.html"
    if not joined.endswith(".html"):
        joined = f"{joined}.html" if not os.path.splitext(joined)[1] else joined
    return joined


def all_html_files(site_dir: Path) -> list[Path]:
    return list(site_dir.rglob("*.html"))


def main():
    site_dir = SITE_DIR
    if not site_dir.exists():
        print(f"Missing _site: {site_dir}")
        return 1

    # Map URL path (no baseurl) -> file path in _site
    def url_path_to_file(url_path: str) -> Path:
        url_path = url_path.split("#")[0].rstrip("/")
        if not url_path or url_path == "index" or url_path == "index.html":
            return site_dir / "index.html"
        if not url_path.endswith(".html"):
            url_path += ".html"
        return site_dir / url_path.lstrip("/")

    visited = set()
    to_visit = ["index.html"]
    broken = []

    while to_visit:
        rel = to_visit.pop(0)
        if rel in visited:
            continue
        visited.add(rel)
        filepath = site_dir / rel if not rel.startswith("/") else site_dir / rel.lstrip("/")
        if not filepath.exists():
            broken.append((None, rel, "page not found (linked from crawl)"))
            continue
        try:
            for href, _ in hrefs_from_html(filepath):
                if not same_origin(href):
                    continue
                if href.startswith("#"):
                    continue  # same page, no file check
                resolved = path_from_href(href, rel)
                if resolved is None:
                    continue
                target_file = site_dir / resolved if not resolved.startswith("/") else site_dir / resolved.lstrip("/")
                if not target_file.exists():
                    broken.append((rel, href, f"target file missing: {target_file.relative_to(site_dir)}"))
                else:
                    # Normalize for queue (relative from _site)
                    try:
                        target_rel = str(target_file.relative_to(site_dir))
                    except ValueError:
                        target_rel = resolved
                    if target_rel not in visited:
                        to_visit.append(target_rel)
        except Exception as e:
            broken.append((rel, "", str(e)))

    # Report
    if broken:
        print("Broken links:")
        for from_page, href, msg in broken:
            print(f"  From: {from_page or '?'}  Link: {href!r}  -> {msg}")
        return 1
    print("All links OK.")
    return 0


if __name__ == "__main__":
    exit(main())

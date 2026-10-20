from pathlib import Path
import json
import shutil
import tempfile
import unittest
import zipfile
from package_studio import package


class PackageTests(unittest.TestCase):
    def setUp(self):
        self.temp=tempfile.TemporaryDirectory();self.root=Path(self.temp.name)
        self.source=self.root/'marketplace';shutil.copytree(Path(__file__).parent/'marketplace',self.source,ignore=shutil.ignore_patterns('__pycache__'))
        self.plugin=self.source/'plugins/konglomerat-game-studio';self.output=self.root/'kit.zip'
    def tearDown(self): self.temp.cleanup()
    def test_complete_kit_is_reproducible_and_contains_registry_and_three_skills(self):
        first=package(self.source,self.output);second=package(self.source,self.root/'second.zip')
        self.assertEqual(first['sha256'],second['sha256'])
        with zipfile.ZipFile(self.output) as archive:
            self.assertIsNone(archive.testzip());names=archive.namelist()
            self.assertIn('konglomerat-local/.agents/plugins/marketplace.json',names)
            self.assertEqual(3,sum(name.endswith('/SKILL.md') for name in names))
    def test_stale_compatibility_identity_is_rejected_before_output(self):
        path=self.plugin/'.codex-plugin/plugin.json';data=json.loads(path.read_text());data['name']='old-name';path.write_text(json.dumps(data))
        with self.assertRaises(ValueError): package(self.source,self.output)
        self.assertFalse(self.output.exists())
    def test_private_env_file_is_rejected(self):
        (self.plugin/'.env').write_text('PRIVATE_SETTING=example')
        with self.assertRaises(ValueError): package(self.source,self.output)
        self.assertFalse(self.output.exists())
    def test_existing_archive_is_preserved(self):
        self.output.write_bytes(b'prior artifact')
        with self.assertRaises(ValueError): package(self.source,self.output)
        self.assertEqual(b'prior artifact',self.output.read_bytes())
    def test_archive_cannot_pollute_source(self):
        with self.assertRaises(ValueError): package(self.source,self.source/'kit.zip')
    def test_python_cache_is_excluded(self):
        cache=self.plugin/'scripts/__pycache__';cache.mkdir();(cache/'private.pyc').write_bytes(b'cache')
        package(self.source,self.output)
        with zipfile.ZipFile(self.output) as archive: self.assertFalse(any('__pycache__' in name for name in archive.namelist()))
    def test_registry_cannot_select_a_sibling_directory(self):
        outside=self.root/'outside';outside.mkdir()
        path=self.source/'.agents/plugins/marketplace.json';data=json.loads(path.read_text());data['plugins'][0]['source']['path']='../outside';path.write_text(json.dumps(data))
        with self.assertRaises(ValueError): package(self.source,self.output)
        self.assertFalse(self.output.exists())


if __name__=='__main__': unittest.main()

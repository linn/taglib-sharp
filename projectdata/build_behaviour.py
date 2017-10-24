# Defines the build behaviour for continuous integration builds.

import sys
import subprocess
import os
import re
import shutil
import tarfile
import glob

try:
    from ci import (OpenHomeBuilder, require_version)
except ImportError:
    print "You need to update ohDevTools."
    sys.exit(1)

require_version(42)


class Builder(OpenHomeBuilder):
    output_dir = "build/packages"
    build_dir = "build"

    def setup(self):
        self.nuget_server = self.env.get('NUGET_SERVER')
        self.nuget_api_key = self.env.get('NUGET_API_KEY')
        self.set_nuget_sln("taglib-sharp.sln")

    def clean(self):
        if os.path.isdir(self.build_dir):
            shutil.rmtree(self.build_dir)

    def build(self):
        if self.platform == 'Windows-x86':
            self.msbuild('taglib-sharp.sln', target='Build', configuration=self.configuration, platform="""Any CPU""")
            if not os.path.isdir(self.output_dir):
                os.makedirs(self.output_dir)
            self.pack_nuget('src/taglib-sharp.nuspec', '.')

        
    def publish(self):
        if self.platform == 'Windows-x86':
            self.publish_nuget(os.path.join('build', 'packages', '*.nupkg'), self.nuget_api_key, self.nuget_server)
